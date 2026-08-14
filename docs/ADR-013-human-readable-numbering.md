# ADR-013: Allocate human-readable document numbers from a per-company counter table

**Status:** Accepted (implemented for estimates)
**Supersedes:** the `COUNT(*) + 1` scheme originally used for estimate numbers.
**Revisit when:** a company wants a custom numbering format (prefix, padding,
or a starting value other than 1).

## Context

[ADR-006](ADR-006-uuid-identifiers.md) settles that primary keys are UUIDs.
Customers, however, cannot read a UUID over the phone, so estimates, jobs, and
invoices also need a short number a person can say out loud: `EST-0042`.

The first implementation counted existing rows:

```csharp
var count = await db.Estimates.CountAsync(e => e.CompanyId == companyId, ct);
var candidate = $"EST-{count + 1 + attempt:D4}";
```

That has three problems, in increasing order of seriousness:

1. **It races.** Two concurrent creates both count *n* and both propose *n+1*.
   The retry loop narrowed the window but could not close it, because counting and
   inserting were separate statements with no lock between them.
2. **It is wrong after a deletion.** Delete one estimate and the next `COUNT(*)`
   reissues a number that was already sent to a customer.
3. **It does not generalise.** Jobs and invoices need the same behaviour, and the
   obvious path was to copy the method and change the prefix — three copies of a
   subtly broken algorithm.

The third point is what makes this an architecture decision rather than a bug fix.

## Decision

**A `company_sequences` table keyed by `(CompanyId, Prefix)`, allocated through
`ICompanySequenceAllocator` inside the caller's transaction.**

```
company_sequences(CompanyId, Prefix, LastValue)  PK (CompanyId, Prefix)
```

- Allocation is a single `UPDATE company_sequences SET last_value = last_value + 1`.
  Postgres takes a row lock that is held until the caller's transaction commits,
  so concurrent allocations for the same company queue rather than reading the
  same value.
- **The caller must be in a transaction**, alongside the insert that consumes the
  number. Outside one, the lock is released immediately and the race returns. This
  is documented on the interface, and `EstimateService.CreateAsync` opens the
  transaction explicitly.
- **The counter is independent of row counts**, so deleting a document never
  reissues its number.
- **One prefix per document family** — `EST`, `JOB`, `INV` — declared in
  `SequencePrefixes`. Jobs and invoices reuse the allocator rather than
  reimplementing it.
- **The first allocation for a `(company, prefix)` seeds the row**, and a lost
  insert race falls through to the normal increment path. There is no
  onboarding step to forget.
- **The unique index on `(CompanyId, EstimateNumber)` stays** as a last-resort
  backstop. If the allocator is ever misused, the database rejects the duplicate
  instead of two customers receiving `EST-0007`.

## Alternatives Considered

- **A Postgres `SEQUENCE` per company** — the database's own answer to this, and
  genuinely fast, since sequences do not lock. Rejected because sequences are not
  transactional: a rolled-back insert consumes the number anyway, leaving gaps.
  Customer-facing document numbers with holes in them prompt "did you lose my
  estimate?" phone calls. It would also mean issuing DDL at runtime for every new
  company.
- **A single global sequence with the company id in the display string** — no
  contention at all. Rejected because the number leaks total platform volume:
  a company's first estimate being `EST-3184` tells them how many other customers
  the product has.
- **Optimistic concurrency with a row version and retry** — portable across
  providers, no reliance on lock semantics. Rejected because it turns contention
  into retry storms under load and needs a provider-specific concurrency token
  (`xmin`) to work properly on Postgres anyway.
- **Compute the number at read time from creation order** — no storage, no
  allocation. Rejected because the number would change if an earlier document
  were deleted, and a number that changes after being sent is not a number.
- **UUID or random short code** (`EST-7F3K9`) — no coordination at all. Rejected
  because sequential numbers are the point: contractors and customers use them to
  refer to "the estimate before last".

## Consequences

- Creating a document now requires an explicit transaction. Any future call site
  that skips it reintroduces the race, which is why the requirement is stated on
  the interface rather than left as folklore.
- Allocation serialises per company. That is the correct trade — a single
  contractor is not creating estimates concurrently in a way that makes lock
  contention meaningful — but it would not scale to a high-volume shared tenant.
- Numbers are gap-free only if the transaction commits. A failed insert rolls the
  counter back with it, which is the behaviour a plain `SEQUENCE` could not give.
- Correctness rests on row-level locking, which SQLite cannot meaningfully
  exercise. This is verified in the Postgres-backed suite
  (`PostgresBehaviourTests`), including a concurrent-create test that the previous
  scheme fails.
- Custom formats (different padding, a company-chosen starting number) are not
  supported. `LastValue` is stored separately from its formatting, so adding them
  later is a display change plus a seed value, not a migration of existing
  numbers.
