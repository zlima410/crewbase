# ADR-005: React + Vite SPA instead of Next.js

**Status:** Accepted

## Context

The web dashboard is entirely authenticated, internal-facing (office
manager/owner) software. There is no public marketing site, no SEO
requirement, and no content that needs to be indexed or rendered for
anonymous visitors.

## Decision

Build the web dashboard as a React + TypeScript SPA using Vite, deployed as
static assets to Cloudflare Pages.

## Alternatives Considered

- **Next.js** — server-side rendering and file-based routing are valuable
  for public, SEO-sensitive, or content-heavy sites. None of that applies to
  an authenticated dashboard. It would add a server-rendering runtime,
  hosting complexity, and debugging surface with no corresponding benefit
  for this app.
- **Create React App** — effectively unmaintained at this point; Vite is the
  more current, faster alternative for the same "plain SPA" use case.

## Consequences

- Deployable as pure static files — simpler hosting, no application server
  for the frontend, cheaper and easier to reason about for a solo developer.
- No server-side rendering means no SEO benefit, which is fine — there's
  nothing public to index in the MVP.
- A public marketing site, if ever needed, can be a separate, simple static
  site later — it does not need to share infrastructure with the dashboard.
