# ADR-014: Bill room area on dimensions rounded up to whole feet

**Status:** Provisional — **not validated with a real contractor.**
**Revisit when:** [`docs/contractor-validation-interview.md`](contractor-validation-interview.md)
has been run. This is the first thing that interview should settle.

## Context

`RoomMeasurement` computes billable area as:

```
Area     = ceil(LengthFeet) × ceil(WidthFeet)
Billable = Area × (1 + WastePercentage / 100)
```

So a 10.5 × 12.5 ft room bills as 11 × 13 = 143 sq ft, not 131.25 — about 9%
more, *before* the waste allowance is applied on top.

This was implemented as though it were a maths decision. It is not. It is a
pricing decision that changes what a customer is charged, and it was made without
asking anyone who quotes flooring for a living. It is recorded here so it stops
being an invisible assumption buried in a value object.

The specific worry is **double-counting**. Rounding dimensions up already builds
in a margin for offcuts, and then a 10% waste percentage is applied on top of the
inflated figure. If contractors think of "round up" and "waste percentage" as
alternative ways to express the same allowance, the current model charges for it
twice.

## Decision

Keep `ceil` per dimension for now, and treat it as provisional.

Reasoning:

- It is defensible on its own terms: flooring is cut from boards and boxes, and
  partial feet are not usable in practice.
- It is the more conservative direction for the contractor, who is the paying
  customer of this product.
- Reversing it later is a formula change in two files plus their tests, not a
  schema migration — the stored `SquareFeet` and `BillableSquareFeet` columns are
  computed values that can be recalculated.

What this ADR commits to is **making the assumption visible and cheap to
change**, not that the rule is right.

## Alternatives Considered

- **Exact area** (`length × width`, no rounding) — mathematically honest, and lets
  the waste percentage be the single, explicit allowance. This is the most likely
  outcome of the interview, since it makes the allowance visible to the customer
  as one number instead of two hidden ones.
- **Round the resulting area**, not each dimension (`ceil(length × width)`) — far
  smaller adjustment: 131.25 becomes 132 rather than 143. Arguably the best of the
  three if any rounding is wanted at all.
- **Round to the nearest half or quarter foot** — closer to how rooms are actually
  measured with a tape. More faithful, but it needs a real answer about
  measurement precision before it means anything.
- **Make it a per-company setting** — sidesteps the decision. Rejected for now:
  configuration is not a substitute for understanding the domain, and one pilot
  company cannot fill in a settings screen it does not understand either.

## Consequences

- The formula is duplicated deliberately: `RoomMeasurement` on the server and
  `web/src/features/estimates/pricingMath.ts` for the live preview. Both cite this
  ADR, and both have unit tests asserting the same worked examples, so a change to
  one that is not mirrored in the other fails a test rather than showing the user
  a different number than gets saved.
- `SquareFeet` and `BillableSquareFeet` are persisted per room. If the rule
  changes, existing estimates keep the figures they were quoted at — which is
  correct, since a sent estimate is a commitment — but a data-fix decision will
  be needed for drafts.
- Until the interview happens, every estimate total the product produces is built
  on an unvalidated pricing rule. This is the largest known correctness risk in
  the MVP, and it cannot be closed by testing.
