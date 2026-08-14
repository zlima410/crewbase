# Architecture decision records

One file per decision, numbered in the order they were made. A record explains
the context, what was chosen, what was rejected and why, and what the choice
costs — so a future reader can tell a deliberate trade-off from an accident.

| ADR                                                   | Decision                                             | Status      |
| ----------------------------------------------------- | ---------------------------------------------------- | ----------- |
| [001](ADR-001-use-postgresql.md)                      | PostgreSQL via Supabase as the primary database       | Accepted    |
| [002](ADR-002-use-supabase-auth.md)                   | Supabase Auth for identity                            | Accepted    |
| [003](ADR-003-modular-monolith.md)                    | Modular monolith, not microservices                   | Accepted    |
| [004](ADR-004-rest-over-graphql.md)                   | REST, not GraphQL                                     | Accepted    |
| [005](ADR-005-vite-spa-not-nextjs.md)                 | React + Vite SPA, not Next.js                         | Accepted    |
| [006](ADR-006-uuid-identifiers.md)                    | UUID primary keys                                     | Accepted    |
| [007](ADR-007-free-tier-hosting.md)                   | Free-tier hosting, portable by design                 | Accepted    |
| [008](ADR-008-no-payment-processing.md)               | No payment processing in the MVP                      | Accepted    |
| [009](ADR-009-mobile-strategy.md)                     | Responsive web first, native iOS second               | Accepted    |
| [010](ADR-010-photo-upload-strategy.md)               | Direct-to-Storage photo uploads via signed URLs       | Accepted    |
| [011](ADR-011-estimate-acceptance-transaction.md)     | Acceptance creates the job in one idempotent transaction | Accepted |
| [012](ADR-012-scheduling-time-zones.md)               | UTC instants, per-company IANA zone, local rendering  | Accepted    |
| [013](ADR-013-human-readable-numbering.md)            | Per-company counter table for document numbers        | Accepted    |
| [014](ADR-014-area-rounding.md)                       | Bill area on dimensions rounded up to whole feet      | Provisional |

## Not yet decided

- **Error reporting / observability.** Console logging only today. Deferred until
  there is a real user whose bug reports need reproducing.
- **Soft delete semantics.** `docs/data-model.md` prefers soft deletion but no
  entity implements it yet; the first `DELETE` endpoint forces the decision.
- **PDF generation for estimates and invoices.** Depends on the answer to
  "how does an estimate reach the customer" in
  [the validation interview](contractor-validation-interview.md).

## Open risks

- [ADR-014](ADR-014-area-rounding.md) is provisional and affects every estimate
  total the product produces. It is unvalidated with a real contractor and cannot
  be closed by testing — see
  [`contractor-validation-interview.md`](contractor-validation-interview.md).
