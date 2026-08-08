# ADR-008: No payment processing in the MVP

**Status:** Accepted
**Revisit when:** the pilot confirms payment *status visibility* isn't
enough, or a customer is paying and Stripe integration is prioritized.

## Context

Discovery (see `discovery.md`, Q8) found the operational problem is
**payment visibility**, not payment infrastructure: the owner needs to know
whether a deposit was received and whether an invoice is paid, without
consulting a separate accounting tool. It did *not* surface a need for the
app itself to move money.

## Decision

Track invoice/payment **status** only — `Draft`, `Sent`, `PartiallyPaid`,
`Paid`, `Overdue`, `Void` — with manual status changes made by office staff.
No Stripe, ACH, or other processor integration in the MVP.

## Alternatives Considered

- **Integrate Stripe now** — real product value eventually, but adds PCI
  scope, webhook handling, and reconciliation complexity before it's known
  whether contractors even want in-app payment collection versus their
  existing accounting software. Explicitly deferred per Section 22 and the
  Non-Goals list (Section 36).
- **Integrate QuickBooks directly** — solves the "two systems" problem
  more completely, but is a bigger integration than the MVP needs to prove
  the core workflow. Listed as a Post-MVP candidate.

## Consequences

- Invoice records are a status ledger the office updates manually, not a
  source of financial truth — accounting software remains authoritative for
  actual money movement.
- Crew-facing views should not surface financial detail by default, per the
  MVP's role permissions.
- If pilot feedback shows manual status updates are themselves a source of
  error or delay, that's the signal to prioritize a real payment/accounting
  integration — not before.
