# Database

## Technology

SQL Server. ORM: Entity Framework Core. Use EF Core migrations.

## General Rules

Use:

- Guid identifiers
- UTC timestamps
- foreign keys
- useful indexes
- explicit relationships
- soft delete where appropriate

Avoid dangerous cascade deletes for business history.

## BaseEntity

Common fields:

```text
Id
CreatedAt
UpdatedAt
```

## TenantEntity

Tenant-owned entities include:

```text
CompanyId
```

## AuditableEntity

Where useful:

```text
CreatedBy
UpdatedBy
```

## Soft Delete

Where appropriate:

```text
IsDeleted
DeletedAt
DeletedBy
```

---

# Company

Fields:

```text
Id
Name
Slug
Phone
Email
IsActive
CreatedAt
UpdatedAt
```

Company represents a tenant. Slug should be unique. ---

# ApplicationUser

Use ASP.NET Core Identity. Additional fields:

```text
CompanyId
FullName
IsActive
ManagerId
CreatedAt
UpdatedAt
```

ManagerId is optional. SuperAdmin requires explicit platform-level handling. ---

# Lead

Fields:

```text
Id
CompanyId
FullName
Phone
Email
Source
Status
BudgetMin
BudgetMax
PreferredLocation
PropertyType
AssignedAgentId
Notes
CreatedAt
UpdatedAt
IsDeleted
DeletedAt
DeletedBy
```

Statuses:

```text
New
Contacted
Interested
Viewing
Negotiation
Reserved
Contracted
Lost
```

---

# LeadActivity

Fields:

```text
Id
CompanyId
LeadId
UserId
Type
Description
ActivityDate
CreatedAt
```

Types:

```text
Call
WhatsApp
Email
Meeting
Viewing
Note
FollowUp
```

---

# Project

Fields:

```text
Id
CompanyId
Name
Developer
Location
Description
StartingPrice
Status
CreatedAt
UpdatedAt
IsDeleted
```

---

# Unit

Fields:

```text
Id
CompanyId
ProjectId
UnitCode
PropertyType
Price
Area
Bedrooms
Bathrooms
Floor
Location
Status
DownPayment
InstallmentYears
Description
CreatedAt
UpdatedAt
IsDeleted
```

Statuses:

```text
Available
Reserved
Sold
Unavailable
```

---

# Deal

Fields:

```text
Id
CompanyId
LeadId
UnitId
SalesAgentId
DealValue
Status
ReservationDate
ContractDate
Notes
CreatedAt
UpdatedAt
```

Statuses:

```text
Pending
Reserved
Contracted
Cancelled
```

---

# Commission

Fields:

```text
Id
CompanyId
DealId
AgentId
CommissionPercentage
CommissionAmount
CompanyCommission
Status
PaymentDate
CreatedAt
UpdatedAt
```

---

# TaskItem

Do not name the entity `Task` because it conflicts conceptually with .NET Task. Fields:

```text
Id
CompanyId
Title
Description
AssignedToUserId
LeadId
DealId
DueAt
Priority
Status
ReminderAt
CreatedAt
UpdatedAt
```

LeadId and DealId can be nullable. ---

# Notification

Fields:

```text
Id
CompanyId
UserId
Type
Title
Message
IsRead
CreatedAt
```

---

# RefreshToken

Fields:

```text
Id
UserId
TokenHash
ExpiresAt
CreatedAt
RevokedAt
ReplacedByTokenId
CreatedByIp
RevokedByIp
```

Never persist the plaintext refresh token. ---

# AuditLog

Fields:

```text
Id
CompanyId
UserId
Action
EntityName
EntityId
OldValues
NewValues
IpAddress
CreatedAt
```

Audit important changes to:

- Leads
- Deals
- Units
- Users
- Commissions

---

# Relationships

```text
Company
 ├── Users
 ├── Leads
 ├── Projects
 ├── Units
 ├── Deals
 ├── Tasks
 └── Notifications
ApplicationUser
 ├── Assigned Leads
 ├── Lead Activities
 ├── Deals
 └── Tasks
Lead
 ├── Lead Activities
 └── Deals
Project
 └── Units
Unit
 └── Deals
Deal
 └── Commissions
```

---

# Important Indexes

## Leads

```text
(CompanyId, Status)
(CompanyId, AssignedAgentId)
(CompanyId, CreatedAt)
(CompanyId, Phone)
(CompanyId, Source)
```

## Projects

```text
(CompanyId, Name)
(CompanyId, Status)
```

## Units

```text
(CompanyId, ProjectId)
(CompanyId, Status)
(CompanyId, Price)
(CompanyId, PropertyType)
```

## Deals

```text
(CompanyId, Status)
(CompanyId, SalesAgentId)
(CompanyId, CreatedAt)
```

## Tasks

```text
(CompanyId, AssignedToUserId, Status)
(CompanyId, DueAt)
```

## Notifications

```text
(CompanyId, UserId, IsRead)
```

## Constraints

- Company Slug must be unique.
- UnitCode should be unique within its relevant company/project scope.
- Use appropriate foreign key delete behaviors.
- Preserve historical deal data.

---

# Later-Phase Entities

Added in Phases 12/19–21 (Billing, WhatsApp, Marketing, Public API, Marketplace, Payments, Media). All are tenant-owned (`CompanyId`) except where noted.

## SubscriptionPlan

Global catalog, not tenant-owned — SuperAdmin-managed, seeded via `HasData`.

```text
Id
Code
Name
MonthlyPrice
MaxUsers
MaxLeads
MaxUnits
IsActive
```

## CompanySubscription

One row per company.

```text
Id
CompanyId
PlanId
Status          (Trialing, Active, Cancelled)
TrialEndsAt
CurrentPeriodStart
CurrentPeriodEnd
CancelledAt
```

## WhatsAppTemplate

```text
Id
CompanyId
Name
Body            ({{FullName}}, {{PreferredLocation}}, {{PropertyType}} placeholders)
IsActive
```

## WhatsAppMessage

```text
Id
CompanyId
LeadId
SentByUserId
TemplateId       (nullable — ad-hoc messages have none)
ToPhone
Body
Status           (Queued, Sent, Failed)
ErrorMessage
SentAt
CreatedAt
```

## Campaign

One-shot bulk broadcast to a Lead segment.

```text
Id
CompanyId
Name
Channel          (Email, WhatsApp)
Subject          (nullable — WhatsApp has no subject)
Body
TargetStatus     (nullable Lead status filter)
TargetSource     (nullable Lead source filter)
Status           (Draft, Sent)
CreatedByUserId
SentAt
RecipientCount
SuccessCount
FailureCount
```

## CampaignRecipient

Immutable per-lead delivery record.

```text
Id
CompanyId
CampaignId
LeadId
Success
ErrorMessage
SentAt
```

## ApiKey

```text
Id
CompanyId
Name
KeyPrefix        (shown in UI; full key never persisted in plaintext)
HashedKey        (SHA-256)
Scopes           (read, or read,write)
IsActive
CreatedByUserId
LastUsedAt
ExpiresAt
```

## WebhookSubscription

```text
Id
CompanyId
Url
Secret           (HMAC signing secret — shown once at creation)
EventTypes       (comma-separated: lead.created, lead.status_changed, deal.contracted)
IsActive
CreatedByUserId
```

## WebhookDelivery

One row per delivery attempt — audit trail.

```text
Id
CompanyId
WebhookSubscriptionId
EventType
Payload
AttemptNumber
Success
ResponseStatusCode
ErrorMessage
CreatedAt
DeliveredAt
```

## Payment

```text
Id
CompanyId
DealId
Amount
Currency
Status                     (Pending, Paid, Failed)
GatewayCheckoutSessionId
GatewayPaymentIntentId
CreatedByUserId
PaidAt
```

## ProjectImage / UnitImage

Same shape for both, scoped to their parent entity.

```text
Id
CompanyId
ProjectId / UnitId
BlobPath
Url
FileName
ContentType
SizeBytes
CreatedAt
```

## Document

```text
Id
CompanyId
BlobPath
Url
FileName
ContentType
SizeBytes
UploadedByUserId
LeadId          (nullable)
DealId          (nullable)
CreatedAt
```

## Unit — additional field

`IsPubliclyListed` (bool, default false) was added in Phase 21 — opt-in flag exposing a unit on the unauthenticated `/api/marketplace/units` surface. See `docs/roadmap.md` Phase 21. `UpdatedAt` is an EF Core optimistic concurrency token (no schema change — it's an EF-metadata concern, not a column type change) — closes a real double-booking race where two concurrent Reserve calls against the same unit could both read `Status == Available` before either wrote. The loser now gets a 409 instead of silently overwriting the winner. See `docs/roadmap.md`'s QA-pass phase and `UnitConfiguration.cs`.

## Deal — additional fields

Four nullable ML feature-snapshot columns were added in Phase 22, populated once at deal creation for training `MlConversionScorer`:

```text
FeatureSnapshotBudgetFit
FeatureSnapshotLocationMatch
FeatureSnapshotPropertyTypeMatch
FeatureSnapshotPriceToBudgetRatio
```
