# ADR-011: Estimate acceptance creates the job in one idempotent transaction

**Status:** Accepted (not yet implemented — decided ahead of Phase 5)
**Revisit when:** acceptance needs to trigger anything outside the database
(email, SMS, calendar sync), at which point the outbox note below stops being
optional.

## Context

Accepting an estimate is the first operation in the product that changes more
than one thing at once:

- the estimate's status becomes `Accepted`
- a `Job` is created from it, with a job number
- the estimate's rooms are copied into the job as the agreed scope

Every write so far has been a single-entity update where a failure leaves nothing
half-done. This one can leave an estimate marked `Accepted` with no job, or a job
with no accepted estimate behind it — and the office manager will only find out
later, on the phone with the customer.

It is also the operation most likely to be double-submitted. Acceptance happens
during a phone call, on a flaky connection, on a button someone taps twice.

## Decision

**One database transaction, guarded by a status precondition and a uniqueness
constraint, so a retry is a no-op rather than a second job.**

- `POST /api/v1/estimates/{id}/accept` performs status transition, job creation,
  and scope copying inside a single explicit transaction. Either all of it
  commits or none of it does.
- **The transition is conditional.** Only an estimate in `Sent` may be accepted.
  The status check and the update happen in the same transaction, so two
  concurrent requests cannot both observe `Sent`.
- **`Job.EstimateId` carries a unique index.** This is the real guarantee. Even
  if the status check is somehow bypassed, the database refuses a second job for
  the same estimate.
- **A repeat call returns the existing job with `200`, not an error.** If the
  estimate is already `Accepted` and a job exists for it, the caller gets that
  job. Retrying a request whose response was lost should not look like a failure.
- Job numbers come from `ICompanySequenceAllocator` with the `JOB` prefix, so
  the number is allocated inside the same transaction as the insert that uses it
  (see [ADR-013](ADR-013-human-readable-numbering.md)).
- **Room scope is copied, not referenced.** The job stores its own rooms. An
  estimate is a quote at a point in time; if it were later revised, a job
  pointing at live estimate rooms would silently change the agreed scope.

## Alternatives Considered

- **Two calls from the client** (`PUT` status, then `POST` job) — no transaction
  needed and each endpoint stays trivial. Rejected outright: the client is a
  browser on a job site, and a lost second call leaves the exact inconsistency
  this ADR exists to prevent.
- **Client-supplied idempotency key** (`Idempotency-Key` header with stored
  responses) — the general solution, and the right one for payments. Rejected as
  overkill here: an estimate can only ever produce one job, so the estimate id
  *is* a natural idempotency key, and the unique index enforces it for free.
- **Domain events / an outbox table**, with job creation handled asynchronously —
  the correct pattern once acceptance must also send an email or push a calendar
  entry, because those cannot join a database transaction. Rejected for now:
  nothing external happens on acceptance yet, and an outbox adds a background
  processor, retry semantics, and an at-least-once delivery model to reason
  about. Worth revisiting the moment notifications land.
- **Optimistic concurrency token on the estimate** (`xmin` as a row version) —
  would also prevent the double transition. Rejected as redundant given the
  conditional update plus the unique index, and it is Postgres-specific in a way
  the rest of the model has avoided.

## Consequences

- This is the first place a transaction spans multiple aggregates, so the
  boundary needs to stay explicit and narrow. It should not become the pattern
  for ordinary CRUD.
- `Job.EstimateId`'s unique index means an estimate can never produce a second
  job. If "re-open a job from a revised estimate" is ever wanted, it becomes a
  new estimate rather than a second job on the old one — which is the honest
  model anyway.
- Copying room scope duplicates data between estimates and jobs. That is
  deliberate; the two answer different questions ("what did we quote" versus
  "what did we agree to build") and will legitimately diverge as change orders
  arrive.
- Returning `200` with the existing job on a repeat call means the endpoint is
  not strictly `201`-on-create. Documented in `docs/api.md` when implemented.
- The Postgres-backed test suite is where this gets verified. Concurrent
  acceptance is not testable against SQLite, for the same reason number
  allocation is not.
