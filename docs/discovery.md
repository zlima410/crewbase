# Discovery — Small Hardwood Flooring Contractor

**Project:** Service Business Job Manager  
**Vertical:** Hardwood flooring  
**Discovery type:** Contractor interview (my dad)
**Date:** August 7, 2026

## Interview Persona

**Business:** Small residential hardwood flooring company  
**Team:** Owner/operator, and 2–4 installers/helpers depending on workload  
**Primary work:** Hardwood installation, refinishing, repairs, stairs, and occasional engineered flooring  
**Customer base:** Mostly homeowners, with some builders, remodelers, and real-estate referrals
**Operating style:** The owner still estimates jobs and is heavily involved in day-to-day operations.

The business is successful enough to have multiple jobs moving at once, but not large enough to justify a dedicated operations department. Information is spread across the owner's phone, texts, email, calendar, accounting software, photos, and occasionally paper notes.

---

# Interview

## 1. How do leads reach you?

Most leads come from a mix of:

- Google Business Profile / Google search
- referrals from previous customers
- builders and remodelers
- phone calls
- text messages
- website contact form
- Facebook/Instagram

Referrals are usually the best leads.

The problem is that leads do not enter through one system. Someone might call while I am on a job, another person texts pictures, and another submits the website form. If I am busy sanding a floor, I might write a name in my Notes app or tell them I will call back.

There is no consistent pipeline from:

```text
New Lead
   ↓
Contacted
   ↓
Estimate Scheduled
   ↓
Estimate Sent
   ↓
Won / Lost
```

A lead can fall through the cracks simply because I forgot to follow up.

### Pain points

- Leads arrive through too many channels.
- Follow-ups are easy to forget.
- I cannot quickly see every open opportunity.
- Customer photos may be in a text conversation before a customer record even exists.
- I sometimes have to search my phone to remember who contacted me.

### Product opportunity

A simple lead inbox/pipeline could eventually be valuable, but full CRM functionality is not required for the first MVP.

---

## 2. How do you record measurements?

Usually I go to the house myself.

I carry a laser measure and sometimes a tape measure. I walk room by room and record:

- room name
- length and width
- approximate square footage
- closets
- hallways
- stairs
- transitions
- existing flooring
- subfloor condition
- floor vents
- furniture
- demolition requirements
- baseboards/shoe molding
- moisture concerns
- installation direction
- repair areas

How I record it varies.

Most of the time it is:

- Notes on my iPhone

A typical note might look like:

```text
Johnson

Living 18x22
Dining 12x14
Hall 4x16
Closet 3x6

Existing oak
Refinish all
3 boards damaged near kitchen
Need shoe molding
Dark walnut stain?
```

Then later I have to turn those notes into an actual estimate.

### Pain points

- Measurements are not standardized.
- Handwritten notes can be hard to interpret later.
- I may forget why I added extra square footage.
- Room measurements and photos live in different places.
- Special conditions discovered during the walkthrough can be forgotten during estimating.
- Re-entering measurements creates opportunities for mistakes.

### Product opportunity

A flooring-specific mobile measurement workflow is highly valuable.

Each room should support:

```text
Room
Dimensions
Calculated Sq Ft
Waste %
Billable Sq Ft
Work Type
Existing Floor
New Floor
Special Conditions
Notes
Photos
```

The system should calculate square footage automatically while still allowing manual adjustment.

---

## 3. How do you calculate estimates?

Square footage is the starting point, but flooring estimates are not simply:

```text
Square Feet × One Price
```

Different parts of the job may have different pricing.

For an installation I may calculate:

```text
Material
+ Installation labor
+ Tear-out
+ Disposal
+ Floor preparation
+ Leveling
+ Moisture barrier
+ Trim / shoe molding
+ Transitions
+ Stairs
+ Furniture moving
+ Delivery
+ Miscellaneous materials
```

For refinishing:

```text
Sand/refinish sq ft
+ stain
+ finish choice
+ repairs
+ stairs
+ shoe molding
+ furniture
+ difficult access
```

I normally know my common rates, but special jobs require adjustments.

For material ordering, I also add waste. A straightforward rectangular installation might need around 5–10%, while complicated layouts could need more.

The estimate ultimately needs to be understandable to the homeowner without showing every internal calculation.

### Pain points

- Re-entering measurements takes time.
- Easy to forget an add-on such as removal or furniture moving.
- Pricing may differ between jobs.
- Material waste calculations can be inconsistent.
- Estimates can take too long to prepare after a busy day.
- I need internal costing details that I may not want customers to see.

### Product opportunity

The estimate builder should have reusable flooring line items while allowing custom items.

Example:

```text
Living Room — 330 billable sq ft

Engineered White Oak       330 × $6.50
Installation               330 × $3.50
Existing Floor Removal     300 × $1.25
Floor Prep                  Flat $250
Shoe Molding                72 lf × $2.00
```

The system should separate **internal calculations** from the **customer-facing estimate**.

---

## 4. How do you schedule jobs?

Mostly through Apple Calendar.

Once a customer accepts, I look at:

- crew availability
- estimated job duration
- material arrival
- customer availability
- other jobs
- drying/curing time
- whether another trade needs to finish first

Flooring schedules move frequently.

A three-day refinishing job may become four days. Materials can be delayed. A customer may postpone. Another contractor may not be finished.

I often end up moving calendar events around manually and texting the crew.

### Pain points

- Calendar shows time but not enough job context.
- It is hard to see crew workload.
- Schedule changes have to be communicated manually.
- Multi-day jobs are cumbersome.
- Material delays can break the schedule.
- There is no obvious connection between the estimate, customer, and calendar event.

### Product opportunity

A job should own its schedule.

The office should be able to see:

```text
Job
Customer
Address
Crew
Start Date
Expected End
Current Status
Material Status
```

A simple week/list view is enough initially.

---

## 5. How do installers receive job information?

Usually through text messages and conversations.

I might send the crew lead:

```text
Johnson tomorrow 8am
123 Main St
Refinish living/dining/hall
Dark walnut
Call me when you get there
```

If there are photos or unusual instructions, I send those separately.

Sometimes I print something, but usually everything is on their phones.

The problem is that details are spread across messages.

### Pain points

Installers may not know:

- exact scope
- square footage
- which rooms are included
- stain/finish choice
- whether furniture must move
- whether demolition is included
- access instructions
- customer phone number
- where materials are located
- special repair instructions

### Product opportunity

The field app should answer one question immediately:

> **"What exactly are we doing at this house today?"**

The crew's Job Detail screen should contain all operational information without requiring them to understand estimating/accounting.

---

## 6. How do you store customer/job photos?

Mostly in the iPhone Photos app.

Sometimes pictures are:

- texted between employees,
- saved in a customer text thread,
- uploaded to Google Drive,
- placed in an album,
- or left mixed into thousands of personal/work photos.

We take pictures for:

- estimates
- existing damage
- moisture issues
- subfloor problems
- before condition
- progress
- repairs
- stain samples
- completed floors

Photos are important because they protect us if a customer later says we damaged something.

### Pain points

- Finding an old photo is difficult.
- Photos lose context.
- Employee photos may never reach the owner.
- Before and after photos get mixed together.
- It is hard to know which room a photo belongs to.

### Product opportunity

Photos should belong directly to a job and support:

```text
Before
Prep
Installation
Sanding
Staining
Finish
Damage
After
Other
```

Ideally they can also be associated with a room.

---

## 7. What causes the most mistakes?

Most mistakes are communication mistakes rather than somebody not knowing how to install flooring.

Common examples:

### Measurement errors

A room, closet, hallway, or waste percentage gets missed.

### Forgotten add-ons

Examples:

- furniture moving
- floor removal
- disposal
- transitions
- trim
- stairs
- repairs
- floor leveling

### Finish/stain confusion

The customer chooses one color or finish and ends up changing their mind later.

### Schedule communication

A job moves and somebody is still working from the old schedule.

### Missing photos

Nobody photographed existing damage before work began.

### Payment misunderstandings

Someone does not know whether the deposit has been collected or what remains due.

### Product opportunity

The biggest opportunity is not sophisticated automation.

It is establishing **one source of truth for each job**.

---

## 8. How do you track deposits and final payments?

Usually through accounting/invoicing software, bank records, checks, card payments, or notes.

A typical arrangement could be:

```text
Estimate: $8,000

Deposit: $4,000
Balance: $4,000
```

The exact payment schedule depends on the job.

The operational problem is that the job information and payment information may live in different systems.

Before starting, I want to know:

> Did we receive the deposit?

When the job is finished:

> Has the final invoice been sent?

And later:

> Has the customer paid?

### Pain points

- Payment status is separated from job status.
- Crew members should not necessarily see all financial information.
- Easy to forget follow-up on an outstanding balance.
- Accounting software may contain the payment but not operational context.

### Product opportunity

The MVP does not need to process money.

It needs clear status:

```text
Estimate Total
Deposit Required
Deposit Received
Invoice Status
Amount Paid
Balance Remaining
```

Actual payment processing/accounting integrations can come later.

---

## 9. What information do crews regularly have to call the office for?

This happens more than it should.

Typical questions:

- What's the customer's address?
- What's their phone number?
- Which rooms are we doing?
- Are we moving the furniture?
- Are we removing the old floor?
- What stain did they choose?
- Which finish are we using?
- How many coats?
- Are the stairs included?
- Are we replacing shoe molding?
- Who is supplying the material?
- Where is the material?
- Did the customer approve this repair?
- Is this damage already documented?
- What are we doing about this subfloor?
- What was the square footage?
- When is the next job?
- Did the customer already pay the deposit?

Some calls are unavoidable because conditions change once flooring is removed.

But a lot of them happen because the original information never made it from the estimate to the crew.

### Product opportunity

If the field app eliminates even a few unnecessary calls per job, that is tangible value for the owner.

---

## 10. Which software do you currently use?

A realistic small contractor might use a combination of:

- Apple Calendar
- Gmail
- iPhone Messages
- Apple Notes
- QuickBooks
- Google Drive
- spreadsheets
- phone calculator
- estimating/invoicing software (Invoice2Go)
- Google Business Profile
- social media

Some companies use dedicated field-service software, but smaller flooring businesses often assemble their workflow from general-purpose tools.

The problem is not necessarily that each tool is bad.

The problem is:

```text
Lead        → one place
Measurements→ another
Estimate    → another
Schedule    → another
Photos      → another
Messages    → another
Payments    → another
```

There is no single job record connecting everything.

---

## 11. What do you dislike about it?

The biggest complaint is fragmentation.

I do not want another complicated enterprise system that takes weeks to configure.

I want to open a customer and immediately see:

```text
Customer
  ↓
Property
  ↓
Estimate
  ↓
Job
  ├── Scope
  ├── Rooms
  ├── Schedule
  ├── Crew
  ├── Notes
  └── Photos
  ↓
Invoice / Payment Status
```

Other complaints:

- Too many clicks.
- Software built for every industry feels generic.
- Features I never use make the product harder to navigate.
- Per-user pricing becomes expensive as I add helpers.
- Mobile apps sometimes expose too much office information to installers.
- Estimate builders can be cumbersome while standing in someone's house.
- Information has to be entered multiple times.
- Setup/configuration can take longer than simply continuing to use spreadsheets.
- Some software feels designed for large HVAC/plumbing companies rather than a small flooring contractor.

### What I would actually want

> "Give me something simple enough that I can learn it tonight and my installer can understand it tomorrow morning."

---

# Key Discovery Findings

## Finding 1 — The central problem is fragmented information

The strongest hypothesis is not:

> Flooring companies need better invoicing software.

It is:

> **Small flooring companies lack one reliable job record connecting the office, estimate, schedule, and field crew.**

This should drive the product architecture.

---

## Finding 2 — Estimate data should become job data automatically

The same information is repeatedly needed:

```text
Measurement
     ↓
Estimate
     ↓
Job Scope
     ↓
Crew Instructions
```

Re-entering it wastes time and creates errors.

The product should preserve this information throughout the lifecycle.

---

## Finding 3 — Room-level organization matters

Flooring work naturally revolves around rooms/areas.

The domain model should treat rooms as first-class records rather than hiding everything inside one notes field.

```text
Job
 ├── Living Room
 ├── Dining Room
 ├── Hallway
 ├── Bedroom
 └── Stairs
```

Each area can eventually carry:

- measurements,
- work type,
- material,
- finish,
- notes,
- photos.

---

## Finding 4 — Field users need a different experience

The owner/office needs:

```text
Customers
Estimates
Scheduling
Invoices
Business administration
```

The installer primarily needs:

```text
Today's Jobs
Address
Scope
Rooms
Instructions
Photos
Notes
Status
```

The mobile application should **not** simply reproduce the desktop dashboard.

---

## Finding 5 — Photos are operational records

Photos are not merely attachments.

They document:

- existing conditions,
- damage,
- progress,
- customer selections,
- completed work.

Photo organization belongs in the core job workflow.

---

## Finding 6 — Scheduling is volatile

Flooring jobs frequently move because of:

- material delays,
- drying/curing,
- unexpected floor conditions,
- other trades,
- customer changes,
- jobs taking longer than expected.

The schedule must be easy to change.

Complex scheduling optimization is unnecessary for the MVP.

---

## Finding 7 — The MVP does not need payment processing

The immediate requirement is **payment visibility**, not payment infrastructure.

Start with:

```text
Deposit Required
Deposit Received
Invoice Sent
Balance
Paid
```

Integrate payment/accounting systems only after validation.

---

# Primary User Personas

## Owner / Estimator

Needs to:

- capture customers,
- measure jobs,
- build estimates,
- schedule work,
- monitor active jobs,
- know payment status.

### Main goal

> Run the company without information falling through the cracks.

---

## Office Manager

Needs to:

- answer customer questions,
- locate job information,
- schedule/reschedule work,
- maintain customer records,
- monitor estimates and invoices.

### Main goal

> Find accurate information without calling the owner.

---

## Crew Lead / Installer

Needs to:

- see today's jobs,
- navigate to the property,
- understand exact scope,
- see room information,
- see material/finish instructions,
- add photos,
- document problems,
- update job status.

### Main goal

> Arrive at the job knowing what needs to be done.

---

# Core Problem Statement

> Small hardwood-flooring contractors manage customer and job information across texts, notes, calendars, photos, spreadsheets, and accounting tools. This fragmentation causes missed details, repeated data entry, unnecessary phone calls, scheduling confusion, and mistakes between estimating and field execution.

---

# MVP Value Proposition

> **Create the estimate once, then carry the same job information all the way through scheduling, field work, photos, completion, and payment tracking.**

---

# Recommended MVP Workflow

```text
CUSTOMER
   │
   ▼
PROPERTY
   │
   ▼
ESTIMATE
   │
   ├── Rooms
   ├── Measurements
   ├── Scope
   ├── Pricing
   └── Photos
   │
   ▼
ACCEPTED
   │
   ▼
JOB
   │
   ├── Schedule
   ├── Crew
   ├── Rooms / Scope
   ├── Notes
   └── Photos
   │
   ▼
IN PROGRESS
   │
   ▼
COMPLETED
   │
   ▼
INVOICE / PAYMENT STATUS
```

---

# MVP Priority Recommendations

## P0 — Must Work

1. Authentication and company isolation
2. Customer records
3. Properties/job sites
4. Room measurements
5. Flooring-specific estimate creation
6. Estimate-to-job conversion
7. Job scheduling
8. Crew assignments
9. Mobile-friendly job details
10. Job status
11. Job notes
12. Before/progress/after photos
13. Basic invoice/payment status

---

## P1 — Strong Next Features

1. Estimate PDF
2. Deposit tracking
3. Room-specific photos
4. Customer signatures
5. Reusable estimate line items
6. Material/finish selections
7. Better weekly calendar
8. Lead pipeline

---

## P2 — Post-Validation

1. SMS notifications
2. Customer portal
3. Online payments
4. QuickBooks integration
5. Automated reminders
6. Crew time tracking
7. Material catalog
8. Profitability reporting
9. Review requests
10. Push notifications

---

# Assumptions That Must Be Validated With Real Contractors

This simulated interview creates hypotheses, not validated requirements.

The following questions should receive special attention in real interviews:

- Do contractors actually measure room-by-room, or primarily use total square footage?
- How commonly are estimates priced per room versus line item?
- Which flooring-specific fields are essential versus unnecessary?
- How much estimate pricing varies between contractors.
- Whether crew leads should see pricing/payment information.
- Whether deposits are typically percentage-based, milestone-based, or manually determined.
- How important offline access is at job sites.
- Whether crews commonly use iPhones, Android devices, or both.
- Whether contractors already pay for Jobber, Housecall Pro, ServiceTitan, QuickBooks, or flooring-specific software.
- What would make them switch rather than continue using their current tools?
- What monthly price would feel obviously worthwhile?
- Which single workflow currently wastes the most owner time?

---

# Hypotheses to Test

## Hypothesis 1

**If estimate information automatically becomes field job information, contractors will save time and make fewer communication errors.**

Measure:

- duplicate data entry avoided,
- crew clarification calls,
- missing-scope incidents.

---

## Hypothesis 2

**A room-based flooring estimate workflow will be faster than generic estimate software.**

Measure:

- time to create estimate,
- corrections required,
- contractor preference.

---

## Hypothesis 3

**Centralized job photos will provide enough operational value to drive regular field usage.**

Measure:

- photos uploaded per job,
- frequency old photos are retrieved,
- whether crews voluntarily use the feature.

---

## Hypothesis 4

**A simplified field interface will improve adoption among installers.**

Measure:

- jobs opened by crew,
- status updates,
- notes/photos submitted,
- office calls for information.

---

# Discovery-Based Product Principle

The MVP should optimize for:

> **"I shouldn't have to enter or explain the same information twice."**

When considering a feature, ask:

1. Does it reduce duplicate work?
2. Does it prevent information from being lost?
3. Does it help the office and field stay synchronized?
4. Does it reduce a common mistake?
5. Will a contractor actually use it during a normal workday?

If the answer to all five is no, it probably does not belong in the MVP.

---

# Next Discovery Step

Do **not** treat this document as completed customer discovery.

Use it to create a better real interview.

Interview at least two real flooring contractors and compare their answers against these hypotheses.

For each finding, classify it:

```text
CONFIRMED
PARTIALLY CONFIRMED
REJECTED
NEW FINDING
```

Then update the MVP backlog.

The most valuable discovery will be where actual contractors disagree with this document.
