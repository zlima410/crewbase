# ADR-001: Use PostgreSQL (via Supabase) as the primary database

**Status:** Accepted

## Context

The MVP needs a relational database for tightly related, invariant-heavy data
(customers → properties → estimates → rooms → jobs → invoices), a $0
starting cost, and a realistic path to production without a rewrite. The
solo developer already knows relational modeling and EF Core.

## Decision

Use PostgreSQL, hosted on Supabase's free tier, accessed via EF Core with the
Npgsql provider. Use EF Core migrations from day one — no manual schema
mutation against anything resembling a production database.

## Alternatives Considered

- **MongoDB / document store** — job/customer/estimate data is inherently
  relational (foreign keys, referential integrity matter for tenancy
  isolation). Fighting a document model to express that isn't worth it.
- **SQLite** — fine for local dev, not for a hosted multi-user app with
  concurrent writes from two clients.
- **Self-hosted Postgres** — real infra work (backups, patching, uptime) the
  MVP doesn't need yet; Supabase's free tier removes that entirely for now.

## Consequences

- Free tier limits (500 MB database, per Section 38 of the MVP plan) are
  workable for a single pilot company but must be watched.
- EF Core + Npgsql is mature and well-documented; no exotic tooling risk.
- Migrating off Supabase later (managed Postgres elsewhere, e.g. Azure) is a
  connection-string change, not an application rewrite, as long as no
  Supabase-specific SQL features are relied upon.
