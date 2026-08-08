# ADR-003: Build the API as a modular monolith

**Status:** Accepted

## Context

The domain (customers, estimates, jobs, scheduling, invoices) has clear
feature boundaries but also shares tenancy, auth, and cross-cutting concerns.
The team is one solo developer working part-time. Operational complexity has
a direct, personal cost — every extra moving piece is something one person
has to deploy, monitor, and debug alone, on top of a full-time job.

## Decision

Build a single deployable ASP.NET Core API, internally organized by vertical
feature area (Customers, Estimates, Jobs, Scheduling, Invoices) rather than
split into independent services.

```text
FlooringManager.Api             (Controllers, Auth, Program.cs)
FlooringManager.Application     (Customers, Estimates, Jobs, Scheduling, Invoices)
FlooringManager.Domain          (Customers, Jobs, Estimates, Shared)
FlooringManager.Infrastructure  (Persistence, Storage, Authentication)
```

Dependency direction is one-way: `Api → Application → Domain`, with
`Infrastructure` implementing `Application`-defined interfaces.

## Alternatives Considered

- **Microservices per domain area** — would let features scale/deploy
  independently, but adds network calls, distributed transactions, service
  discovery, and multiple deployment pipelines for a system with one pilot
  customer. Explicitly out of scope per the plan's non-goals (Section 36).
- **A single flat project with no internal structure** — faster to start,
  but makes it harder to reason about where a change belongs as the domain
  grows past customers/estimates/jobs.

## Consequences

- One deployment, one set of logs, one health check — matches a solo
  developer's actual operational capacity.
- Feature boundaries inside the monolith make a future extraction to
  services possible later, if a specific area (e.g., photo processing)
  ever demonstrably needs to scale independently — but that's a Section 65
  "upgrade trigger," not a day-one requirement.
- If the four-project split (`Api`/`Application`/`Domain`/`Infrastructure`)
  ever slows development down, collapse `Application` and `Domain` before
  adding more projects, not after.
