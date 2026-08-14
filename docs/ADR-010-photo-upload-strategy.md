# ADR-010: Upload job photos directly to Supabase Storage with signed URLs

**Status:** Accepted (not yet implemented — decided ahead of Phase 7)
**Revisit when:** photos need server-side processing (thumbnails, EXIF stripping,
OCR), or storage moves off Supabase.

## Context

Job photos are the first feature that is infrastructure-heavy rather than
CRUD-heavy: large binary payloads, slow and unreliable mobile uploads, and files
that must be as strictly tenant-isolated as the database rows are. Crew members
upload from phones on job sites, frequently on poor connections.

The API currently runs on a free tier with modest request timeouts and limited
memory. Proxying multi-megabyte uploads through it would put the slowest,
least reliable traffic in the app's critical path.

Photos also differ from every other record so far in one important way: the
database row and the bytes can get out of sync. Either can be created without
the other.

## Decision

**Upload direct from client to Supabase Storage using a short-lived signed
upload URL issued by the API. The bytes never pass through the API.**

The flow:

1. Client `POST /api/v1/jobs/{jobId}/photos` with content type and byte size.
2. API authorizes the caller against the job's `CompanyId`, validates the
   declared type and size, allocates a `JobPhoto` row in `Pending` state with a
   server-chosen storage path, and returns a signed upload URL plus the photo id.
3. Client `PUT`s the bytes to Supabase Storage.
4. Client `POST /api/v1/jobs/{jobId}/photos/{photoId}/complete`. The API
   verifies the object exists and its reported size matches, then flips the row
   to `Ready`.

Supporting decisions:

- **Storage paths are server-generated**, never client-supplied:
  `company/{companyId}/job/{jobId}/{photoId}.{ext}`. Leading with `companyId`
  makes tenancy a path prefix, so a Storage policy can enforce it and a bug
  cannot write into another company's namespace.
- **A private bucket.** Reads go through short-lived signed download URLs from
  the API, which applies the same company check as every other endpoint. Public
  buckets would make photo URLs guessable and permanent.
- **Content type and size are validated server-side** against an allowlist
  (`image/jpeg`, `image/png`, `image/webp`) and a maximum size, and the signed
  URL is scoped to what was declared.
- **`Pending` rows are the expected failure mode, not an error.** A client that
  dies mid-upload leaves one behind. A sweep deletes `Pending` rows older than
  24 hours along with any orphaned object.
- **Only `Ready` photos are returned** by list endpoints.

## Alternatives Considered

- **Proxy uploads through the API** (`multipart/form-data` to an endpoint that
  forwards to Storage) — simplest to reason about, one round trip, and tenancy is
  enforced in code that already exists. Rejected because it makes the API's
  memory and request-timeout limits the binding constraint on photo size, and it
  is the failure mode most likely to be hit on a job site.
- **Store images as `bytea` in Postgres** — genuinely simpler: one transaction,
  no orphan states, no second system to authorize. Rejected on the free tier's
  500 MB database limit; a few hundred job photos would exhaust it, and backups
  would balloon.
- **Client-side upload straight to Storage using the user's Supabase session and
  RLS policies** — no API involvement at all. Rejected because tenancy would then
  be enforced by Storage policies expressed in SQL, duplicating the `CompanyId`
  rules that live in C#. Two independent implementations of the same
  authorization rule is exactly how tenant leaks happen.
- **A third-party image service (Cloudinary, imgix)** — free tiers exist and
  thumbnails come for free. Rejected as a third vendor to configure and reason
  about before there is any evidence photos need transformation.

## Consequences

- Photo upload is a three-call flow, so the client needs real state handling:
  retry, progress, and cancellation. This is the first feature where "it worked
  on my laptop" is not a meaningful test.
- `Pending` rows and orphaned objects are a normal state that needs a cleanup
  job. Without it, storage leaks quietly.
- There is a window where Storage holds bytes with no `Ready` row. Reads are
  filtered by status, so this is invisible to users, but the cleanup sweep is
  what stops it accumulating.
- Signed URLs expire, which means download links cannot be cached or emailed.
  That is the intended trade for not having permanently guessable photo URLs.
- Thumbnails are out of scope. Full-size images will be served to the list view
  initially, which will need revisiting once a real job has 40 photos on it.
- Because uploads bypass the API, the API cannot enforce the declared size —
  only verify it afterwards. A caller could upload a smaller or larger object
  than declared; the completion step is what catches the mismatch.
