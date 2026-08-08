# ADR-009: Responsive web first, native iOS second

**Status:** Accepted
**Revisit when:** a real pilot company wants the native app, or TestFlight/
App Store distribution is justified.

## Context

Field workers need a usable mobile experience, but Apple requires a paid
Developer Program membership ($99/year) for standard App Store/TestFlight
distribution. The project's constraint is $0 until the product is validated,
so there is no honest way to distribute a native iOS app to real external
users for free.

## Decision

- **Phase 1:** the React web dashboard is built responsively and covers the
  full field workflow (My Jobs, Job Detail, Update Status, Add Note, Upload
  Photo) so it's usable in Safari on an iPhone at $0. In parallel, the
  SwiftUI app is developed and tested only via Simulator and personal
  developer-device installs — no external distribution yet.
- **Phase 2 (post-validation):** pay for the Apple Developer Program only
  once a real company wants the native app, or TestFlight/App Store
  distribution is actually needed.

## Alternatives Considered

- **Native-first** — better on-device experience (camera handling, offline
  affordances, push notifications later), but blocked from reaching real
  external users without the paid Apple program, which would mean paying
  before any validation has happened.
- **Cross-platform framework (React Native / Flutter)** — could unify web
  and mobile code, but the plan is explicitly iOS-only for the MVP (no
  Android per the Non-Goals list) and the team already has React (web) and
  wants real Swift experience — a cross-platform tool would serve neither
  goal well here.

## Consequences

- The responsive web app is not a "fallback" in name only — it must
  actually be good enough for a crew member to use daily, since it's the
  only $0 distribution path during validation.
- The native app is intentionally smaller than the web dashboard by design
  (Section 12/28 of the MVP plan) — it should not attempt to reproduce the
  full office experience.
- Swift development should not start in earnest until the API contract has
  stabilized against the web app (Phase 9), to avoid double rework.
