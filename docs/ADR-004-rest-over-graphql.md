# ADR-004: Use REST, not GraphQL

**Status:** Accepted

## Context

Two clients (React SPA, SwiftUI app) need to consume the API. Each screen's
data needs are fairly well known up front (e.g., "My Jobs" needs a specific
shape, "Job Detail" needs a specific, larger shape) rather than being highly
variable or client-driven.

## Decision

Expose a conventional REST/JSON API under `/api/v1`, with explicit
request/response DTOs per endpoint (see `api.md`).

## Alternatives Considered

- **GraphQL** — better suited to highly variable client queries and
  avoiding over/under-fetching across many client shapes. For two clients
  with well-understood, relatively stable screens, it adds schema tooling,
  resolver complexity, and a steeper learning curve without a matching
  payoff. Explicitly listed as a non-goal in the MVP plan (Section 4).
- **RPC-style (e.g., gRPC)** — strong typing and performance benefits, but
  weaker browser support and more tooling overhead than a REST API a junior
  developer already knows well.

## Consequences

- Endpoint design must be intentional about what each screen needs (e.g.,
  Job Detail returns rooms/assignments/photos/notes together) since there's
  no client-driven field selection.
- Swagger/OpenAPI comes essentially for free with ASP.NET Core, giving
  self-documenting endpoints without extra tooling investment.
- If a specific screen's data needs become genuinely variable later, that's
  a signal to revisit — not a reason to adopt GraphQL up front.
