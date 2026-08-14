# crewbase

Run your service business from one place.

The first product is a job manager for hardwood flooring contractors: customers,
properties, estimates, jobs, scheduling, and invoicing. See
[`docs/hardwood-flooring-job-manager-mvp.md`](docs/hardwood-flooring-job-manager-mvp.md)
for the build plan and [`docs/architecture.md`](docs/architecture.md) for the
system overview.

Architecture decisions are indexed in [`docs/decisions.md`](docs/decisions.md).
The largest open risk is recorded there and in
[`docs/contractor-validation-interview.md`](docs/contractor-validation-interview.md):
the estimate pricing rules have not been validated with a working contractor.

## Layout

| Path   | What it is                                                     |
| ------ | -------------------------------------------------------------- |
| `api/` | ASP.NET Core Web API — modular monolith, EF Core, Postgres     |
| `web/` | React + TypeScript + Vite SPA (office dashboard, responsive)   |
| `ios/` | SwiftUI field app — deliberately smaller than the web app, see ADR-009 |
| `docs/`| Plan, architecture, data model, API reference, and ADRs        |

## Prerequisites

- .NET SDK 10.0
- Node.js 22+
- A Supabase project (free tier) for Postgres, Auth, and Storage
- Docker — only for the Postgres-backed test suite

## Configuration

Nothing secret is committed, and there is no example secrets file to copy: an
`appsettings.Secrets.example.json` is easy to fill in and accidentally commit
under a slightly different name, so the API reads secrets from
[.NET user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
in development and from environment variables everywhere else.

### API

```bash
cd api

dotnet user-secrets set "ConnectionStrings:SupabaseDb" \
  "Host=db.<project-ref>.supabase.co;Database=postgres;Username=postgres;Password=<password>;SSL Mode=Require" \
  --project src/FlooringManager.Api

dotnet user-secrets set "Supabase:Issuer" \
  "https://<project-ref>.supabase.co/auth/v1" \
  --project src/FlooringManager.Api

dotnet user-secrets set "Supabase:Audience" "authenticated" \
  --project src/FlooringManager.Api
```

In hosted environments set the same keys as environment variables, using `__`
for the separator (`ConnectionStrings__SupabaseDb`, `Supabase__Issuer`).

Non-secret settings live in `appsettings.json` and can be overridden per
environment:

| Key                                | Purpose                                                            |
| ---------------------------------- | ------------------------------------------------------------------ |
| `Cors:AllowedOrigins`              | Exact origins allowed to call the API. Empty means same-origin only; `*` is rejected. |
| `RateLimiting:Enabled`             | Master switch for request limiting.                                |
| `RateLimiting:PermitLimit`         | Requests per caller per window, all endpoints.                     |
| `RateLimiting:WindowSeconds`       | Length of the fixed window.                                        |
| `RateLimiting:IdentityPermitLimit` | Tighter budget for `/api/v1/me` and future auth endpoints.         |

Limits partition by authenticated subject where available and by remote IP
otherwise, so one tenant cannot exhaust another's budget.

### Web

Copy `web/.env.example` to `web/.env.local` and fill it in. The app validates
these at startup and fails with a readable message rather than sending requests
to `undefined`. The Supabase anon key is a public, RLS-scoped client key and is
expected to ship in the bundle — never put a service-role key here, since Vite
inlines `VITE_*` values into JavaScript that anyone can read.

## Running

```bash
# Once, so the browser trusts the local API over https
dotnet dev-certs https --trust

# API — https://localhost:7069, OpenAPI UI at /scalar/v1 in Development
cd api && dotnet run --project src/FlooringManager.Api --launch-profile https

# Web — http://localhost:5173
cd web && npm install && npm run dev
```

## Database migrations

EF Core migrations are the only way the schema changes.

```bash
cd api
dotnet tool restore

dotnet ef migrations add <Name> \
  --project src/FlooringManager.Infrastructure \
  --startup-project src/FlooringManager.Api

dotnet ef database update \
  --project src/FlooringManager.Infrastructure \
  --startup-project src/FlooringManager.Api
```

## Tests

```bash
cd api

# Unit tests plus the SQLite-backed endpoint suite
dotnet test --filter "Category!=Postgres"

# Endpoint suite against a real Postgres in Docker: verifies the migrations
# apply, that search is genuinely case-insensitive, and that number allocation
# survives concurrent writers
dotnet test --filter "Category=Postgres"

cd ../web
npm run lint
npm test      # pricing math, asserting the same worked examples as the server
npm run build
```

The web pricing tests exist because
`web/src/features/estimates/pricingMath.ts` mirrors the server's
`RoomMeasurement`/`RoomPricing`/`EstimatePricing` value objects to drive the live
estimate preview. Both sides assert the same numbers, so a change to one that
isn't mirrored in the other fails a test rather than quietly showing the user a
different total than gets saved.

All of the above runs in [`.github/workflows/ci.yml`](.github/workflows/ci.yml),
with the Postgres suite as its own job because it needs Docker.
