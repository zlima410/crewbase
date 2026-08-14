# Data Model

## Entity Overview

```text
Company
 ├── Users
 ├── Customers
 │    └── Properties
 │
 ├── Estimates
 │    └── EstimateRooms
 │
 ├── Jobs
 │    ├── JobRooms
 │    ├── JobAssignments
 │    ├── JobPhotos
 │    └── JobNotes
 │
 └── Invoices
```

Every entity below `Company` carries (directly or via its parent) a
`CompanyId`. This is the multi-tenancy rule and it is non-negotiable — see
"Tenancy Rule" at the bottom of this document.

---

## Company

```text
Company
- Id
- Name
- Phone
- Email
- CreatedAt
```

Present from day one even with a single pilot company, to avoid a painful
multi-tenant migration later.

## User

```text
User
- Id
- AuthProviderUserId   (maps to Supabase Auth identity)
- CompanyId
- FirstName
- LastName
- Email
- Role                 (Owner | OfficeManager | CrewLead)
- IsActive
- CreatedAt
```

## Customer

```text
Customer
- Id
- CompanyId
- FirstName
- LastName
- Email
- Phone
- Notes
- CreatedAt
- UpdatedAt
```

## Property

A customer can have multiple properties (e.g., primary residence + rental).
Address intentionally lives here, not on `Customer`.

```text
Property
- Id
- CustomerId
- StreetAddress
- City
- State
- PostalCode
- AccessNotes
```

## Estimate

```text
Estimate
- Id
- CompanyId
- CustomerId
- PropertyId
- EstimateNumber       (human-readable, e.g. EST-1001)
- Status                Draft | Sent | Accepted | Rejected | Expired
- CreatedDate
- ExpirationDate
- LaborSubtotal
- MaterialSubtotal
- Tax
- Total
- Notes
```

## EstimateRoom

The flooring-specific core of the domain model.

```text
EstimateRoom
- Id
- EstimateId
- Name
- LengthFeet
- WidthFeet
- SquareFeet
- WastePercentage
- BillableSquareFeet
- FlooringType          SolidHardwood | EngineeredHardwood | ExistingHardwood | Other
- WorkType              NewInstallation | Refinishing | Repair | ScreenAndRecoat | Removal
- InstallationMethod    NailDown | GlueDown | Floating | Existing | Unknown    (nullable)
- FinishType            WaterBased | OilBased | Unfinished | PreFinished | Other  (nullable)
- Notes                 (nullable, max 2000)
- MaterialCostPerSqFt
- LaborCostPerSqFt
```

`InstallationMethod` and `FinishType` are optional — a room can be measured and
priced without them, and on refinishing work the crew often cannot tell until
they are on site. `null` means "not recorded"; `InstallationMethod.Unknown` is a
stronger statement, meaning the existing floor was inspected and the method could
not be identified.

Both enums are deliberately coarse. The MVP does not model a materials catalog, so
anything more specific than these categories — a particular stain or product line
— goes in the room's `Notes`. Blank notes are normalized to `null` on write, so a
cleared field and an omitted one are stored identically.

**Calculations** (deterministic, unit-tested, server-authoritative):

```text
Area          = Length × Width
Billable Area = Area × (1 + WastePercentage / 100)
Labor Cost    = BillableSqFt × LaborRatePerSqFt
Material Cost = BillableSqFt × MaterialRatePerSqFt
Room Total    = Labor Cost + Material Cost
Subtotal      = Sum(Room Totals)
Total         = Subtotal + Tax
```

## Job

Created when an Estimate is accepted. Do not depend on the source Estimate's
mutable data after creation — copy what's needed.

```text
Job
- Id
- CompanyId
- CustomerId
- PropertyId
- EstimateId
- JobNumber             (human-readable, e.g. JOB-1001)
- Status                Scheduled | InProgress | Waiting | Completed | Cancelled
- ScheduledStart
- ScheduledEnd
- ActualStart
- ActualEnd
- Description
- InternalNotes
- CustomerNotes
- CreatedAt
```

## JobRoom

Copied from `EstimateRoom` at acceptance time — an independent snapshot, not a
live reference.

```text
JobRoom
- Id
- JobId
- Name
- SquareFeet
- BillableSquareFeet
- FlooringType
- WorkType
- InstallationMethod
- FinishType
- Notes
```

## JobAssignment

MVP crew management is "assign individual users" — no crew-group subsystem.

```text
JobAssignment
- Id
- JobId
- UserId
- AssignmentDate
- Role
```

## JobNote

Append-only. Never edited or deleted via the API — the job history stays
trustworthy.

```text
JobNote
- Id
- JobId
- AuthorUserId
- Text
- CreatedAt
```

## JobPhoto

Binary lives in Supabase Storage; only metadata lives here.

```text
JobPhoto
- Id
- JobId
- StoragePath
- Category    Before | Prep | Installation | Sanding | Staining | Finish | Damage | After | Other
- Caption
- UploadedByUserId
- CreatedAt
```

## Invoice

No payment processing in MVP — status tracking only.

```text
Invoice
- Id
- CompanyId
- JobId
- InvoiceNumber         (human-readable, e.g. INV-1001)
- Amount
- Status                 Draft | Sent | PartiallyPaid | Paid | Overdue | Void
- DueDate
- PaidDate
- Notes
```

---

## Tenancy Rule

Every business-owned record is reachable back to a `Company`, either directly
(`CompanyId` present) or through its parent (e.g., `Property` → `Customer` →
`Company`).

Every protected query must filter by the authenticated caller's `CompanyId`,
resolved server-side from the validated JWT/User lookup — **never** trusted
from a client-supplied value.

```text
Bad:      GET /api/v1/jobs/123  →  return Job 123

Correct:  Authenticated User
            → Resolve CompanyId server-side
            → Find Job where Id == 123 AND CompanyId == User.CompanyId
```

A request for a record outside the caller's company should behave as if it
doesn't exist (404), not reveal that a different company owns it (403).

## Conventions

- **IDs:** UUID/GUID for all primary keys (see
  [ADR-006](ADR-006-uuid-identifiers.md)). Human-readable numbers
  (`EST-0001`, `JOB-0001`, `INV-0001`) exist only as a separate display field
  for customer-facing communication — they are never the primary key.
- **Document numbering:** allocated from `company_sequences`, keyed by
  `(CompanyId, Prefix)`, through `ICompanySequenceAllocator` inside the caller's
  transaction. Never derived from `COUNT(*)`, which races and reissues numbers
  after a deletion. See [ADR-013](ADR-013-human-readable-numbering.md).
- **Timestamps:** stored in UTC as `timestamptz`. Local-time conversion happens
  at the application boundary only, driven by `Company.TimeZoneId` (an IANA
  identifier). Do not scatter timezone math through the codebase. See
  [ADR-012](ADR-012-scheduling-time-zones.md).
- **Deletion:** prefer soft deletion or omit `DELETE` entirely in the MVP,
  particularly for `Customer` — historical records matter.
- **Constraints:** enforce invariants at the database level where practical
  (`SquareFeet >= 0`, `Amount >= 0`, foreign keys). UI validation is a
  convenience, not the source of truth.
