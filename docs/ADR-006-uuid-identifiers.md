# ADR-006: Use UUIDs for primary keys

**Status:** Accepted

## Context

Records are created by multiple actors (office staff on web, crew on iOS,
potentially offline-adjacent flows later) and are exposed through a REST API
to two clients. Sequential integer IDs are easy to enumerate and can leak
information about record counts/order across a public-ish API surface.

## Decision

Use UUID/GUID identifiers as primary keys for all entities. Pair them with
separate, human-readable display numbers (`EST-1001`, `JOB-1001`,
`INV-1001`) for customer-facing communication — the display number is never
the primary key or used for lookups that matter for authorization.

## Alternatives Considered

- **Auto-incrementing integers** — simpler to read/debug, but easy to
  enumerate (`/jobs/124`, `/jobs/125`, ...) and reveal business volume;
  also awkward if client-generated IDs are ever needed (e.g., offline
  creation flows later).
- **Integers + row-level security only** — authorization should not depend
  on IDs being hard to guess in the first place; this isn't a substitute for
  proper tenant scoping, just a smaller extra layer.

## Consequences

- IDs are not a security boundary by themselves — every query must still be
  explicitly scoped by `CompanyId` (see `data-model.md`'s Tenancy Rule).
  UUIDs reduce enumerability; they don't replace authorization checks.
- Slightly larger index/storage footprint than integers — a non-issue at
  MVP/pilot scale.
- Well supported by both PostgreSQL and EF Core with no extra tooling.
