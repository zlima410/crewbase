# ADR-007: Free-tier hosting for MVP, portable by design

**Status:** Accepted
**Revisit when:** a customer is paying, or uptime becomes operationally
important (see Section 65, Upgrade Triggers, in the MVP plan).

## Context

The project has a hard $0 infrastructure budget until it's validated with a
real pilot company. At the same time, the architecture needs to survive the
transition to paid infrastructure without a rewrite once that validation
happens.

## Decision

- **Web:** Cloudflare Pages (free static hosting, Git-based deploys, CDN,
  HTTPS).
- **API:** a free container/web-service host (e.g., Render's free tier) for
  development, demos, and early pilot use — explicitly *not* promised as
  production-grade. The API stays containerized so it can move to Azure App
  Service, Azure Container Apps, paid Render, AWS, or another host later
  with no application code changes.
- **Database/Auth/Storage:** Supabase free tier (Postgres, Auth, Storage).

## Alternatives Considered

- **Vercel Hobby for the frontend** — current terms restrict Hobby to
  personal/non-commercial use, which doesn't fit a project intended to
  become a real paid product. Cloudflare Pages' free tier has no such
  restriction for this use case.
- **Azure App Service F1 (free)** — Microsoft positions this explicitly for
  development/testing, not production, and applies CPU/resource quotas.
  Useful as a demo alternative, not the long-term free answer.
- **Paying for hosting immediately** — contradicts the explicit $0
  validation-first constraint; premature before there's a paying customer
  to justify the spend.

## Consequences

- Free hosts (Render free tier in particular) can sleep after inactivity and
  have cold-start latency. Acceptable for demos and early validation; **not**
  acceptable once a flooring company depends on the app every morning — that
  is the trigger to upgrade, not a reason to avoid the free tier now.
- No Cloudflare- or Render-specific business logic is allowed to leak into
  the application — hosting must stay swappable.
- Terms and free-tier limits change; re-verify before onboarding a paying
  customer (see Section 38 of the MVP plan for the most recent check).
