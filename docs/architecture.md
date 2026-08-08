# Architecture

## System Overview

```text
                         ┌─────────────────────────┐
                         │   React Web Dashboard   │
                         │   TypeScript + Vite     │
                         │  Owner / Office Manager │
                         └────────────┬────────────┘
                                      │ HTTPS / JSON
┌─────────────────────────┐          │           ┌─────────────────────────┐
│     SwiftUI iOS App     │          │           │     Supabase Auth       │
│  Installer / Crew Lead  │──────────┼──────────▶│  Users + JWT identity   │
└─────────────────────────┘          │           └─────────────────────────┘
                                      ▼
                         ┌─────────────────────────┐
                         │   ASP.NET Core Web API  │
                         │  Business rules + RBAC  │
                         └────────────┬────────────┘
                   ┌──────────────────┼──────────────────┐
                   ▼                  ▼                  ▼
          ┌────────────────┐ ┌────────────────┐ ┌────────────────┐
          │  PostgreSQL    │ │ Supabase       │ │ Logging /      │
          │  via Supabase  │ │ Storage        │ │ Diagnostics    │
          └────────────────┘ └────────────────┘ └────────────────┘
```

Two clients (web, iOS) talk to one ASP.NET Core API over REST/JSON. The API is the
single owner of business rules and authorization. Neither client talks to Postgres
or Storage directly.

## Style: Modular Monolith

One deployable API, organized by vertical feature area rather than by technical
layer sprawl. No microservices, no message broker, no service mesh. See
[ADR-003](decisions/ADR-003-modular-monolith.md).

```text
FlooringManager.Api             → Controllers, Auth, Program.cs
FlooringManager.Application     → Customers, Estimates, Jobs, Scheduling, Invoices
FlooringManager.Domain          → Customers, Jobs, Estimates, Shared
FlooringManager.Infrastructure  → Persistence, Storage, Authentication
```

Dependency direction is one-way: `Api → Application → Domain`.
`Infrastructure` implements interfaces defined in `Application`, it does not sit
underneath it in the call chain.

If this structure slows development down, collapse `Application` and `Domain`
before adding new projects — do not add layers for their own sake.

## Tenancy

Every business-owned record carries a `CompanyId`. All protected queries filter
by the authenticated caller's company, resolved server-side — never trusted from
the client. See `data-model.md` and the Authorization section of `api.md`.

## Identity

Supabase Auth issues JWTs to both clients. The API validates the token on every
protected request and resolves company/role from its own `User` table — Supabase
proves *who*, the API decides *what they can do*. See
[ADR-002](decisions/ADR-002-use-supabase-auth.md).

## Clients

- **Web** — React + TypeScript + Vite SPA, not Next.js. The app is an
  authenticated dashboard with no need for server-side rendering.
- **iOS** — SwiftUI, deliberately smaller than the web app. Field-relevant
  screens only (My Jobs, Job Detail, Status, Notes, Photos). Office/admin
  functionality stays web-only.
- **Mobile web fallback** — the React app is responsive and covers the core
  field workflow (My Jobs, Job Detail, Update Status, Add Note, Upload Photo)
  in Safari, so the product is fully usable at $0 before native distribution
  is justified. See [ADR-009](decisions/ADR-009-mobile-strategy.md).

## Data & Storage

- **PostgreSQL via Supabase**, accessed through EF Core migrations. See
  [ADR-001](decisions/ADR-001-use-postgresql.md).
- **Supabase Storage** holds photo binaries; Postgres holds only metadata
  (`JobPhoto.StoragePath`, category, caption, uploader, timestamp). Binary
  files are never stored in Postgres.

## Hosting (MVP / $0 stage)

```text
main
  ├── Cloudflare Pages   → React SPA (static)
  ├── Free API host      → ASP.NET Core (e.g. Render free tier)
  └── Supabase Free      → Postgres + Auth + Storage
```

Free hosts are for development, demos, and early validation — not for a
business that depends on the system operationally. The API is containerized
so it can move to a paid host without a rewrite. See
[ADR-007](decisions/ADR-007-free-tier-hosting.md).

## What This Architecture Deliberately Avoids

Microservices, Kubernetes, message brokers, event sourcing, CQRS frameworks,
service meshes, Redis, GraphQL, and multiple independent APIs. All are valid
tools — none are appropriate for a solo developer validating an MVP. Revisit
only if a specific, demonstrated need arises (see Section 65 / Upgrade
Triggers in the MVP plan).

## Related Documents

- `data-model.md` — entities, relationships, tenancy rules
- `api.md` — REST conventions and endpoint surface
- `decisions/` — one ADR per major architectural decision
