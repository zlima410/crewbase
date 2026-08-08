# API

## Conventions

- **Style:** REST over JSON.
- **Prefix:** all routes under `/api/v1`.
- **Auth:** every route except health checks requires a valid Supabase JWT in
  the `Authorization: Bearer <token>` header. Invalid/missing token → `401`.
- **Tenancy:** `CompanyId` is always resolved server-side from the
  authenticated user. It is never accepted as a request parameter or body
  field for authorization purposes. A record outside the caller's company
  returns `404`.
- **Authorization:** role checks (Owner / OfficeManager / CrewLead) happen in
  the API layer per the permissions table in `architecture.md`'s source plan.
  Insufficient role on an otherwise-valid request → `403`.
- **DTOs:** EF Core entities are never returned directly. Every endpoint has
  an explicit request/response record. See "DTO Strategy" below.
- **Errors:** standardized error shape (Problem Details). No stack traces,
  connection strings, or internal exception detail in any response.

## DTO Strategy

Explicit request/response models give you a stable public contract that's
decoupled from internal schema changes.

```csharp
public sealed record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string? Email,
    string Phone,
    string? Notes
);

public sealed record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string Phone,
    string? Notes
);
```

## Endpoint Surface

### Current user

```http
GET  /api/v1/me
```
Returns the authenticated user's identity, role, and company. Used by both
clients to bootstrap session state.

### Customers

```http
GET    /api/v1/customers
POST   /api/v1/customers
GET    /api/v1/customers/{id}
PUT    /api/v1/customers/{id}
DELETE /api/v1/customers/{id}    # may be omitted entirely for MVP; prefer soft delete if implemented
```

### Properties

```http
POST /api/v1/customers/{customerId}/properties
PUT  /api/v1/properties/{id}
```

### Estimates

```http
GET  /api/v1/estimates
POST /api/v1/estimates
GET  /api/v1/estimates/{id}
PUT  /api/v1/estimates/{id}

POST /api/v1/estimates/{id}/rooms
POST /api/v1/estimates/{id}/accept   # transactional: Estimate → Accepted, Job created, rooms copied
```

### Jobs

```http
GET   /api/v1/jobs                    # supports ?assignedToMe=true for the iOS "My Jobs" screen
POST  /api/v1/jobs
GET   /api/v1/jobs/{id}
PUT   /api/v1/jobs/{id}

PATCH /api/v1/jobs/{id}/status        # enforces valid status transitions only
POST  /api/v1/jobs/{id}/assignments
POST  /api/v1/jobs/{id}/notes         # append-only
POST  /api/v1/jobs/{id}/photos        # multipart upload → Supabase Storage + metadata row
```

### Schedule

```http
GET /api/v1/schedule?from=YYYY-MM-DD&to=YYYY-MM-DD
```

### Invoices

```http
GET   /api/v1/invoices
POST  /api/v1/jobs/{jobId}/invoice
GET   /api/v1/invoices/{id}
PATCH /api/v1/invoices/{id}/status
```

### Health

```http
GET /health
```
```json
{ "status": "healthy" }
```
No auth required. Add a database-connectivity check when useful, but don't
over-invest here for MVP.

## Notable Server-Side Rules (not just validation — business rules)

- **Estimate acceptance is transactional.** `Estimate.Status → Accepted`,
  `Job` created, `EstimateRoom → JobRoom` copy all succeed together or none
  do. A retry/double-click must not create a second job.
- **Job status transitions are constrained**, not free-form:
  `Scheduled → InProgress → Waiting/Completed`, etc. Invalid transitions are
  rejected, not silently accepted.
- **Job notes are append-only.** No update or delete endpoint.
- **Photo uploads validate file type and size** before touching Storage, and
  always verify the target job belongs to the caller's company.

## Versioning

The `/api/v1` prefix exists so a breaking change can ship as `/api/v2`
alongside it rather than breaking existing clients (particularly the iOS app,
which can't be force-updated the way a web SPA can).

## Related Documents

- `data-model.md` — entity fields these DTOs are built from
- `architecture.md` — where the API sits in the overall system
- `decisions/ADR-004-rest-over-graphql.md` — why REST
