# ADR-002: Use Supabase Auth for identity

**Status:** Accepted

## Context

Both the web dashboard and the iOS app need to authenticate users and obtain
a token the ASP.NET Core API can validate. Building and securing a custom
auth system (password hashing, token issuance, refresh, reset flows) is
significant, security-sensitive work with no product differentiation.

## Decision

Use Supabase Auth as the identity provider for both clients. Clients
authenticate directly with Supabase and receive a JWT, which they attach to
every API request. The ASP.NET Core API validates the JWT on each protected
request but does **not** treat Supabase as the source of truth for
authorization — the API's own `User` table (company, role, active status)
governs what an authenticated identity is allowed to do.

```text
User → Supabase Auth → JWT → ASP.NET Core API → validate token → authorize request
```

## Alternatives Considered

- **Roll our own auth** — more control, but meaningfully more security
  surface area for a solo developer to get right, for no MVP benefit.
- **Auth0 / other third-party IdP** — comparable capability, but Supabase is
  already in use for Postgres + Storage, so one fewer vendor/free-tier
  relationship to manage.
- **ASP.NET Core Identity** — viable, but re-implements what Supabase gives
  for free and adds password-reset/email-verification work the team
  otherwise gets out of the box.

## Consequences

- Identity proof (authentication) and business authorization are cleanly
  separated: Supabase proves *who*, the API decides *what they can do*.
- Service-role keys must never be shipped to the browser or the iOS app —
  only client-safe keys and short-lived JWTs leave the server boundary.
- Free-tier MAU limits (Section 38) are far beyond MVP pilot scale, so no
  near-term cost pressure here.
