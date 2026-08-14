# ADR-012: Store instants in UTC, store the company's IANA time zone, render locally

**Status:** Accepted (partly implemented — decided ahead of Phase 6)
**Revisit when:** a company operates across more than one time zone, or crews
need to see times in a zone different from the office's.

## Context

Everything persisted so far is a `DateTimeOffset` in UTC, which is correct for
audit timestamps (`CreatedAt`, `UpdatedAt`) because nobody cares what local time
a row was written at.

Scheduling breaks that assumption. "Tuesday, 8am" is not an instant — it is a
local wall-clock time that only becomes an instant once you know where the crew
is standing. Getting this wrong produces the classic failures:

- a job scheduled for 8am displays as 3am after a deployment to a server in a
  different region
- a job scheduled across a daylight-saving boundary shifts by an hour
- "today's jobs" returns the wrong set near midnight, because the day boundary
  was computed in UTC

The last one is the subtle one, and it is the query the field app makes most
often.

## Decision

- **Instants are stored in UTC**, as `timestamptz`, via `DateTimeOffset`.
  Unchanged from the current model.
- **`Company` gains an IANA time zone** (`TimeZoneId`, e.g. `America/Chicago`),
  set once at onboarding. This is the zone the business operates in and the one
  its schedule is expressed in.
- **IANA identifiers, not Windows ones.** `America/Chicago`, never
  `Central Standard Time`. IANA names are what the browser, iOS, and Postgres
  all speak.
- **Conversion happens at the edges.** The API accepts and returns instants; the
  client renders them in the company's zone. Domain logic never does time zone
  arithmetic.
- **Day-boundary queries are computed in the company's zone**, then converted to
  a UTC range for the query. "Jobs today" means midnight-to-midnight *local*,
  which is not a fixed 24-hour UTC window across a DST transition.
- **`TimeProvider` stays the only clock.** `DateTime.Now` and
  `DateTime.UtcNow` are never called directly, so tests can pin the clock. This
  is already the convention and scheduling must not break it.
- **Never store a UTC offset as a substitute for a zone.** `-05:00` does not tell
  you whether the next appointment is in daylight saving time; `America/Chicago`
  does.

## Alternatives Considered

- **Store local wall-clock time plus a zone identifier**, converting to an
  instant only when needed — arguably the most faithful model of "Tuesday 8am,
  whatever UTC says". Rejected because every query that orders or ranges over
  time would need conversion first, which is both slow and easy to forget in one
  place out of ten.
- **Store the offset alongside the instant** and reconstruct local time from it —
  cheap, no new column on `Company`. Rejected because an offset is a fact about
  one moment, not a rule: it cannot answer "what is 8am local three weeks from
  now", which is precisely what scheduling asks.
- **Per-user time zones** rather than per-company — more flexible, and where this
  ends up if crews ever cross zones. Rejected for the MVP: a single flooring
  contractor works in one metro area, and per-user zones would mean the office
  and the crew could disagree about which day a job is on.
- **Assume the server's local zone** — zero work. Rejected because it makes
  correctness depend on host configuration, and free-tier hosting does not
  guarantee a region.

## Consequences

- `Company.TimeZoneId` must be populated for every company, including the pilot,
  before scheduling ships. A missing or invalid zone has to fail loudly at
  onboarding rather than silently defaulting to UTC.
- The web and iOS clients both need the company's zone, so `/api/v1/me` should
  return it alongside the company id.
- DST transition weeks are a real test case, not a theoretical one: a schedule
  query spanning the spring-forward boundary covers 23 hours, not 24. This
  deserves explicit unit tests with a pinned `TimeProvider`.
- Storing IANA identifiers means the API depends on the host having up-to-date
  tzdata. On Linux containers that is the OS package; it needs to be part of the
  base image rather than assumed.
- Audit timestamps stay UTC-only with no zone attached, which is correct and
  should not be "fixed" for consistency with scheduling.
