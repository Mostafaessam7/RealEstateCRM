# Project Roadmap

## Current Phase

All 18 planned phases complete, plus all 11 explicitly-requested "Later" items (Phases 19–21): Billing & Subscriptions, WhatsApp Automation, Recommendation Engine, AI Lead Assistant, Subscription Limit Enforcement, Marketing Automation, Public API & Webhooks, Public Marketplace, Online Payments, Advanced (ML) Recommendation Engine, Mobile App — plus a Phase 22 documentation/validation completeness pass, a Phase 23 that replaced the Phase 21 mobile app with a Flutter rewrite and overhauled the web app's visual design system, and a Phase 24 that closed every remaining actionable gap found by a full `.md`-file audit (real email/WhatsApp delivery, CI test gating, and several stale-documentation corrections), and a Phase 25 production-readiness/QA pass that found and fixed one Critical (Android release build had no INTERNET permission), four High (auth rate limiting, refresh-token revocation on password change, a real unit-double-booking race condition, a known-vulnerable transitive package), and several Medium issues (security headers, a modal keyboard-focus trap, frontend code-splitting, CI Flutter gating) across the whole stack, and a Phase 26 that assessed and then implemented moving the web app's refresh token out of `localStorage` into a Secure/HttpOnly/SameSite cookie with CSRF protection — additive only, Flutter and the Public API are completely unaffected, and a Phase 27 that ran the whole stack live for the first time (real SQL Server, real Redis, real browser) and found two severe bugs no prior automated test could reach — every API enum serializing as a raw integer, and the JWT role claim using the wrong claim type so every user's role-gated nav was silently hidden — plus several smaller UI/data issues, all fixed and verified against the running app, and a Phase 28 that added dashboard charts/recent-activity/KPI polish, then reviewed the whole project again and caught a real, systemic unformatted-money-value bug across 7 more pages (a variant of the Phase 27 bug) plus a completely unstyled Reports-page KPI row (referencing a CSS class that never existed), fixing both everywhere at once, and a Phase 29 that found the "Mecodex" brand assets had been dropped into the client folder but never wired into the app (generic favicon/title, no logo anywhere, "Real Estate CRM" hardcoded as the user-facing name) and integrated them across both the web client and the Flutter app (app icon/ launcher name on Android, iOS, and Flutter web). The "Later" list is empty.

## Current Task

All requested phases complete, including the Phase 29 Mecodex brand integration. Nothing is planned or pending — "Later" is empty. Do not start new work without an explicit request.

---

# Phase 1 — Foundation

- [x] Create .NET solution
- [x] Create Domain project
- [x] Create Application project
- [x] Create Infrastructure project
- [x] Create Api project
- [x] Configure project references
- [x] Configure dependency injection
- [x] Configure SQL Server
- [x] Configure EF Core
- [x] Create ApplicationDbContext
- [x] Add base entities
- [x] Add Company entity
- [x] Configure Company entity
- [x] Create initial migration
- [x] Validate solution builds

---

# Phase 2 — Authentication

- [x] Configure ASP.NET Core Identity
- [x] Create ApplicationUser
- [x] Configure Identity tables
- [x] Seed roles
- [x] Implement login
- [x] Implement JWT generation
- [x] Implement refresh token entity
- [x] Implement refresh token rotation
- [x] Implement logout/revocation
- [x] Implement change password
- [x] Implement forgot password
- [x] Implement reset password
- [x] Add authorization policies
- [x] Add authentication tests

---

# Phase 3 — Multi-Tenancy

- [x] Implement ICurrentTenantService
- [x] Resolve CompanyId from authenticated context
- [x] Add CompanyId to tenant entities
- [x] Configure EF Core tenant query filters
- [x] Configure tenant-safe writes
- [x] Handle SuperAdmin explicitly
- [x] Add tenant isolation integration tests

Required tests:

- [x] Tenant A cannot read Tenant B data
- [x] Tenant A cannot update Tenant B data
- [x] Tenant A cannot delete Tenant B data
- [x] guessed IDs cannot bypass tenant isolation

Note: no tenant-owned business entities exist yet (Lead/Unit/Deal land in Phases 4-6), so the
mechanism (ITenantEntity + TenantEntity + EF global query filter + tenant-safe SaveChanges) was
proven with a throwaway TestTenantEntity in the test project. Any future TenantEntity subclass
gets isolation automatically with zero extra wiring.

---

# Phase 4 — Leads MVP

## Entity

- [x] Lead entity
- [x] LeadStatus enum
- [x] LeadSource design
- [x] EF configuration
- [x] indexes

## API

- [x] Lead DTOs
- [x] Lead validators
- [x] Create Lead
- [x] List Leads
- [x] Lead Details
- [x] Update Lead
- [x] Soft Delete Lead

## Querying

- [x] Pagination
- [x] Search
- [x] Status filter
- [x] Agent filter
- [x] Source filter
- [x] Sorting

## Assignment

- [x] Assign lead
- [x] Transfer lead

## Activities

- [x] LeadActivity entity
- [x] Add activity
- [x] Activity timeline

## Follow-ups

- [x] Schedule follow-up
- [x] List upcoming follow-ups

Note: follow-ups reuse LeadActivity (Type = FollowUp, ActivityDate = scheduled time) rather than a
separate entity — POST /api/leads/{id}/activities schedules one, GET /api/leads/follow-ups/upcoming
lists them. No dedicated FollowUp entity was introduced.

---

# Phase 5 — Projects & Inventory

## Projects

- [x] Project entity
- [x] Project CRUD
- [x] Project search
- [x] Project filtering

## Units

- [x] Unit entity
- [x] UnitStatus enum
- [x] Unit CRUD
- [x] Unit search
- [x] Unit filtering
- [x] Unit availability

Note: UnitCode uniqueness (per company/project) is enforced both by an explicit pre-check in
UnitService (works reliably across providers) and a unique EF index as a DB-level backstop.

---

# Phase 6 — Deals

- [x] Deal entity
- [x] DealStatus enum
- [x] Create deal
- [x] Reserve unit
- [x] Contract deal
- [x] Cancel deal
- [x] Deal history
- [x] Deal authorization

Notes:

- Deal history: deals are never hard-deleted (no DELETE endpoint) — Cancelled is a terminal

  status, not a delete, per docs/database.md & "preserve historical deal data". Deals for a given lead/unit can be listed via GET /api/deals?leadId=...&unitId=....

- Deal authorization: CompanyAdmin/SalesManager/SuperAdmin manage any deal; a SalesAgent may

  only create/update/reserve/contract/cancel deals where they are SalesAgentId (403 otherwise). Read access (list/get) is uniformly available to any authenticated tenant user, same simplification as Leads/Projects/Units in earlier phases.

- Extended ICurrentTenantService with IsInRole(role) to support this (used only by Deals so far).

---

# Phase 7 — Commissions

- [x] Commission entity
- [x] Commission calculation
- [x] Agent commission
- [x] Company commission
- [x] Commission status
- [x] Commission payment tracking

Notes:

- One commission per deal, only creatable once its deal is Contracted. CommissionAmount

  (agent) and CompanyCommission are each computed independently from DealValue * their own percentage — there is no single shared "total commission pool" field in docs/database.md, so the two percentages are supplied separately on create and not persisted, only their resulting dollar amounts are.

- Status: Pending -> Paid (mark-paid, sets PaymentDate) or Pending -> Cancelled. No further

  transitions once Paid/Cancelled — financial records are never hard-deleted.

- Authorization: create/mark-paid/cancel require CompanyAdmin, SalesManager, or SuperAdmin

  (reuses the elevated-role check introduced for Deals). Read access is uniform, same simplification as other modules.

---

# Phase 8 — Tasks & Follow-ups

- [x] TaskItem entity
- [x] Create task
- [x] Assign task
- [x] Update task
- [x] Complete task
- [x] Link task to Lead
- [x] Link task to Deal
- [x] Reminder date

Notes:

- Entity named TaskItem (not Task) to avoid colliding with System.Threading.Tasks.Task, per

  docs/database.md. Status enum named TaskItemStatus for the same reason vs System.Threading.Tasks.TaskStatus.

- No delete endpoint — added a Cancel action (Pending -> Cancelled) for symmetry with

  Deals/Commissions, since roadmap only asked for Complete but every other terminal-status entity in this codebase has a Cancel path too.

- ReminderAt is stored on the entity; no reminder job/notification exists yet — that's

  Phase 9 (Hangfire) and Phase 10 (SignalR/notifications).

---

# Phase 9 — Hangfire

- [x] Configure Hangfire
- [x] Configure SQL persistence
- [x] Follow-up reminder jobs
- [x] Task reminder jobs
- [x] Scheduled notifications
- [x] Failed job handling

Notes:

- Added the Notification entity now (fields per docs/database.md) since reminder jobs need

  somewhere to write to — no Notifications API endpoint yet, that's Phase 10 (SignalR)/dashboard territory. LeadActivity/TaskItem each got a ReminderSentAt column so jobs are idempotent (won't re-notify on every 5-minute run).

- Jobs never depend on HttpContext (per docs/multi-tenancy.md#background-jobs): they read

  across all tenants via ApplicationDbContext.ForAllTenants<T>(), group by CompanyId, then open an explicit ITenantScope(companyId) before writing so tenant-safe SaveChanges resolves the correct CompanyId. This also fixes a latent gap — ICurrentTenantService previously had no way to work outside an HTTP request at all.

- Failed job handling: [AutomaticRetry(Attempts = 3)] on both jobs; exhausted retries land in

  the Hangfire dashboard's Failed list. Dashboard is restricted to local requests only for now — needs a real authorization filter before any non-local deployment (Phase 18).

---

# Phase 10 — SignalR

- [x] Configure SignalR
- [x] Authentication
- [x] User-specific notifications
- [x] Tenant groups
- [x] Lead assignment notification
- [x] Follow-up notification
- [x] Deal notification

Notes: NotificationsHub (Infrastructure) is [Authorize]-gated; JWT is read from the querystring (access_token) for the /hubs/notifications path only, since browsers can't set an Authorization header on a WebSocket upgrade. User-specific push uses SignalR's default user-id provider (NameIdentifier claim). Tenant groups: every connection joins tenant:{companyId} on connect. INotificationService.NotifyUserAsync persists a Notification row and pushes live in one call; ReminderJobs (Phase 9) call it too. Lead assignment fires on Assign/Transfer; Deal notification fires on Reserve/Contract/Cancel.

---

# Phase 11 — Redis

- [x] Configure Redis
- [x] Tenant-scoped cache keys
- [x] Company settings caching
- [x] Dashboard caching
- [x] Property search caching
- [x] Cache invalidation

Notes: TenantCacheKeys centralizes tenant:{companyId}:... key formats. Dashboard summary (GET /api/dashboard/summary, 1-minute TTL) and company settings (GET /api/companies/current, 10-minute TTL, read-only — no update-company endpoint exists yet). Property search caching: GET /api/units/available (unfiltered case), invalidated on Unit create/update/delete and on Deal Reserve/Contract/Cancel.

---

# Phase 12 — Azure Blob Storage

- [x] Blob storage service
- [x] Upload abstraction
- [x] Project images
- [x] Unit images
- [x] User avatars
- [x] Documents
- [x] Tenant-scoped blob paths

Notes: IBlobStorageService/AzureBlobStorageService wraps Azure.Storage.Blobs. Dev connection string is the Azurite emulator constant (UseDevelopmentStorage=true). BlobPaths centralizes tenant-scoped path formats (companies/{companyId}/projects/{projectId}/..., etc.) — paths are not an authorization mechanism, every service still checks entity access first. New entities: ProjectImage, UnitImage, Document (immutable, hard-deleted). ApplicationUser got AvatarBlobPath/AvatarUrl. Images: JPEG/PNG/WEBP, 5MB max. Documents: any type, 20MB max.

---

# Phase 13 — React Foundation

- [x] React + TypeScript
- [x] Project structure
- [x] React Router
- [x] TanStack Query
- [x] API client
- [x] React Hook Form
- [x] Zod
- [x] Base layout

Notes: client/real-estate-crm-react scaffolded with Vite. Folder structure matches docs/frontend.md. Axios instance (src/api/client.ts) centralizes base URL, bearer token attachment, and 401-triggered refresh.

---

# Phase 14 — React Authentication

- [x] Login
- [x] Authentication state
- [x] Protected routes
- [x] Role-aware routes
- [x] Access token handling
- [x] Refresh flow
- [x] Logout
- [x] Session expiration handling

Notes: AuthContext decodes the JWT client-side only for UI purposes (nav visibility, user label) — never a security boundary. RoleRoute hides admin-only pages (Users, Company Settings, Commissions) from the nav/routes for other roles; backend authorization is what actually enforces this. On refresh failure the client fires a window event (SESSION_EXPIRED_EVENT) that ProtectedRoute reacts to by redirecting to /login.

---

# Phase 15 — React CRM

- [x] Dashboard
- [x] Leads Table
- [x] Create Lead
- [x] Lead Details
- [x] Lead Timeline
- [x] Pipeline Board
- [x] Projects
- [x] Units
- [x] Deals
- [x] Tasks
- [x] Commissions
- [x] Users
- [x] Company Settings

Notes: Backend gap found and closed: no user-management endpoints existed. Added GET/POST /api/users, PUT /api/users/{id}/role, PUT /api/users/{id}/active (CompanyAdmin/SuperAdmin for mutations). Company Settings is read-only (no update-company endpoint yet). Pipeline Board has no drag/drop, per docs/frontend.md — a "Move to next stage →" button instead. Deal form only offers currently-available units.

---

# Phase 16 — Reports

- [x] Dashboard KPIs
- [x] Leads report
- [x] Sales report
- [x] Conversion report
- [x] Lead source report
- [x] Agent performance
- [x] Commission report
- [x] Inventory report

Notes: Dashboard KPIs extended to match docs/frontend.md#dashboard exactly. New ReportsController/IReportsService/ReportsService — 6 endpoints under /api/reports. Lead source report is folded into the leads report response (BySource). All reports are tenant-scoped via the existing EF global query filters.

---

# Phase 17 — Audit & Security

- [x] AuditLog
- [x] Lead audit
- [x] Deal audit
- [x] Unit audit
- [x] User audit
- [x] Commission audit
- [x] Security review
- [x] Tenant isolation review
- [x] Authorization review

Notes: AuditLog captured automatically inside ApplicationDbContext.SaveChanges(Async) by inspecting ChangeTracker entries for Lead/Deal/Unit/ApplicationUser/Commission — not scattered through services. PasswordHash/SecurityStamp/ConcurrencyStamp explicitly excluded from every snapshot. Read via GET /api/audit-logs, CompanyAdmin/SuperAdmin only. Tenant isolation review: no new findings — covered by the global EF query filter + ~20 dedicated cross-tenant tests. Authorization review: every controller carries a class-level [Authorize] except AuthController's intentionally-public actions. Security review found and fixed one real gap: no CORS policy existed, which would have blocked the React app from reaching the API — added an allow-listed policy (Cors:AllowedOrigins config, never AllowAnyOrigin).

---

# Phase 18 — Docker & Deployment

- [x] Backend Dockerfile
- [x] Frontend Dockerfile
- [x] docker-compose
- [x] SQL Server local container
- [x] Redis local container
- [x] Environment configuration
- [x] Azure deployment configuration

Notes:

- Backend Dockerfile (src/RealEstateCRM.Api/Dockerfile) is multi-stage (SDK build ->

  aspnet runtime), runs as a non-root user, build context must be the repo root (needs all 4 projects for restore). Frontend Dockerfile (client/real-estate-crm-react/Dockerfile) is multi-stage (node build -> nginx), VITE_API_BASE_URL baked in at build time via ARG since Vite env vars are compile-time. nginx.conf falls back unknown paths to index.html for React Router.

- docker-compose.yml wires sqlserver, redis, azurite (local Blob Storage emulator — not in

  the checklist but needed for Phase 12's blob storage to run locally without real Azure), api, and client. Secrets (SQL_SA_PASSWORD, JWT_KEY) come from a gitignored .env file, never hardcoded — .env.example documents the required keys. Migrations are NOT applied automatically on container start (documented as a manual/CI step in docs/deployment.md) — auto-migrating on every boot is risky for a container that might scale to multiple instances.

- Azure deployment configuration: new docs/deployment.md maps the docker-compose environment

  variables onto Azure Web App for Containers settings (SQL, Redis, Storage, Key Vault references for secrets) and a GitHub Actions workflow (.github/workflows/azure-deploy.yml) that builds both images, pushes to ACR, and deploys to two Web Apps on push to main. No real Azure resources were provisioned — this is configuration/CI-as-code, ready to point at real resource names via repo secrets/variables.

- Validated: docker compose config parses cleanly. Docker daemon was not running in this

  environment, so the actual image builds could not be executed end-to-end here — Dockerfiles follow standard, widely-used multi-stage patterns for ASP.NET Core and Vite+nginx.

---

# Phase 19 — Billing & Subscriptions, WhatsApp Automation, Recommendation Engine, AI Lead Assistant

Explicitly requested by the user (previously listed under "Later" as out-of-scope).

### Billing & Subscriptions

- Domain: `SubscriptionPlan` (global catalog, SuperAdmin-managed, seeded Free/Starter/Pro/

  Enterprise via `HasData`) and `CompanySubscription` (one row per company; auto-provisioned on a 14-day Free trial the first time a company accesses `/subscriptions/current`).

- `ISubscriptionService`: get current subscription + live usage (users/leads/units) against

  plan limits, change plan, cancel. `ISubscriptionPlanService`: SuperAdmin plan CRUD.

- Usage is surfaced (progress bars, near-limit highlighting). Hard enforcement at creation

  time was out of scope for this pass and shipped separately in Phase 20 (Subscription Limit Enforcement, below) — this note originally deferred it; it is no longer deferred.

- API: `SubscriptionsController` (`/api/subscriptions/plans`, `/current`, `/change-plan`,

  `/cancel`, SuperAdmin-only plan management under `/plans` POST/PUT and `/plans/all`).

- Frontend: `/billing` page (current plan + usage bars + plan comparison cards), nav item

  for CompanyAdmin/SuperAdmin.

- Migration: `AddSubscriptionsBilling`. Tests: `SubscriptionServiceTests` (5 tests).

### WhatsApp Automation

- Domain: `WhatsAppTemplate` (reusable message templates with `{{FullName}}` /

  `{{PreferredLocation}}` / `{{PropertyType}}` placeholders) and `WhatsAppMessage` (outbound log per Lead: status Queued/Sent/Failed, error message on failure).

- `IWhatsAppSender` abstraction with a `LoggingWhatsAppSender` placeholder (mirrors the

  existing `IEmailSender`/`LoggingEmailSender` pattern) until a real WhatsApp Business API provider is configured — no fabricated credentials.

- API: `WhatsAppController` (`/api/whatsapp/templates` CRUD, `/api/whatsapp/leads/{id}/send`,

  `/api/whatsapp/leads/{id}/messages`).

- Frontend: `/whatsapp-templates` management page; a WhatsApp panel on the Lead details page

  (send box + message history) for CompanyAdmin/SalesManager/SuperAdmin.

- Migration: `AddWhatsAppAutomation`. Tests: `WhatsAppServiceTests` (6 tests).

### Recommendation Engine

- `IRecommendationService.GetRecommendationsForLeadAsync`: rule-based (not ML), explainable

  scoring of Available units against a Lead's budget/location/property-type — budget fit (exact or within 15% tolerance), preferred-location substring match, property-type match. No new tables — computed on demand from existing Leads/Units data.

- API: `GET /api/leads/{id}/recommendations`. Frontend: "Recommended Units" panel on the Lead

  details page with a match-score badge and reasons, linking to each unit.

- Tests: `RecommendationServiceTests` (3 tests).

### AI Lead Assistant

- `IAiLeadAssistantService` with a `TemplateAiLeadAssistantService` implementation — rule-based

  summary / next-best-action / draft follow-up message generated from the lead's own data (status, source, budget, days since last contact). No external LLM API key is configured for this deployment, so this does not call a hosted model; the interface is the extension point for swapping in a real LLM-backed implementation later, behind a configuration-supplied key — never hardcoded.

- API: `GET /api/leads/{id}/ai-insight`. Frontend: "AI Assistant" card on the Lead details page

  with a "Generate insight" action and a "Use as WhatsApp message" shortcut into the WhatsApp send box.

- Tests: `TemplateAiLeadAssistantServiceTests` (3 tests).
- Validated: `dotnet build` (0 errors), `dotnet test` — 115/115 passing (98 prior + 17 new),

  `npm run build` (frontend, clean). Note: usage limits were surfaced but not enforced at creation time in this phase — see Phase 20, which closes that gap.

---

# Phase 20 — Subscription Limit Enforcement, Marketing Automation, Public API & Webhooks

Explicitly requested by the user (previously listed under "Later" as out-of-scope).

### Subscription Limit Enforcement

- `ISubscriptionLimitService`/`SubscriptionLimitService`: checks the current company's plan

  limits (MaxUsers/MaxLeads/MaxUnits) against live usage before a create, throwing `AppException(402 Payment Required)` when the limit is reached. A company with no provisioned subscription yet, or a cancelled one, is handled explicitly (allowed / blocked respectively).

- Wired into `LeadService.CreateAsync`, `UnitService.CreateAsync`, `UserManagementService.CreateAsync`

  via an **optional** constructor parameter defaulting to a permissive `NullSubscriptionLimitService` — avoids touching the 50+ existing direct-construction test call sites across the suite; production DI always resolves the real enforcing implementation.

- No migration — reuses the Phase 19 `CompanySubscription`/`SubscriptionPlan` tables.
- Tests: `SubscriptionLimitServiceTests` (4 tests).

### Marketing Automation

- Domain: `Campaign` (one-shot bulk broadcast to a Lead segment filtered by Status/Source, over

  Email or WhatsApp — not a scheduled/recurring drip sequence, a natural next step) and `CampaignRecipient` (immutable per-lead delivery record: success/failure, error message).

- `ICampaignService`: create as Draft, send (queries matching leads, sends via the existing

  `IEmailSender`/`IWhatsAppSender` abstractions, records one `CampaignRecipient` per lead, transitions to Sent), list delivery history.

- API: `CampaignsController` (`/api/campaigns`, CompanyAdmin/SalesManager/SuperAdmin).

  Frontend: `/marketing-campaigns` page — create, target-segment picker, send, delivery history.

- Migration: `AddMarketingCampaigns`. Tests: `CampaignServiceTests` (4 tests).

### Public API & Webhooks

- **Versioning**: additive `/api/v1/...` surface, separate from the internal `/api/...` routes

  the React SPA uses (unchanged) — a future v2 would ship alongside v1, not replace it.

- **Auth**: dual-scheme — `Authorization: Bearer` (same JWT as the rest of the app, full

  per-user permissions) or `X-Api-Key` (a company-scoped credential for mobile-backend/ server-to-server integrations, `read` or `read,write` scoped, hashed at rest like a GitHub PAT — plaintext shown once at creation). A custom `ApiKeyAuthenticationHandler` resolves the key to the same `ICurrentTenantService` claims the rest of the app already relies on, so tenant isolation and every downstream service work unchanged under either scheme.

- **Rate limiting**: ASP.NET Core's built-in `RateLimiter` middleware, 120 req/min fixed window

  per API key (or user id for Bearer requests), `429` on excess — no extra package.

- **Endpoints**: Leads (full CRUD), Deals/Units/Projects (read), Dashboard summary — thin

  controllers under `Api/Controllers/V1` delegating to the same Application services as the internal API, so pagination/filtering/sorting/validation are identical by construction. `ApiKeysController` (`/api/api-keys`) manages keys, CompanyAdmin/SuperAdmin, JWT-only.

- **Webhooks**: `WebhookSubscription` (URL + HMAC secret + subscribed event types) and

  `WebhookDelivery` (one row per attempt — audit trail). `IWebhookPublisher`, wired into `LeadService`/`DealService` the same optional-parameter/null-object way as the subscription limiter, publishes `lead.created`, `lead.status_changed`, `deal.contracted` — fire-and-forget, never blocks or fails the triggering request. Delivery is a Hangfire job (`WebhookDeliveryJob`): HMAC-SHA256 signs the payload (`X-Webhook-Signature`), POSTs it, and self-schedules up to 3 retries (1m/5m/15m backoff, 4 attempts total) on failure. `WebhooksController` (`/api/webhooks`) manages subscriptions and delivery history, CompanyAdmin/SuperAdmin.

- Frontend: `/api-keys` and `/webhooks` management pages (create, revoke/delete, one-time

  secret reveal, delivery history viewer).

- Documentation: `docs/public-api.md` — versioning, auth, rate limits, endpoint list, webhook

  payload/signature/retry contract.

- Migration: `AddPublicApiAndWebhooks`. Tests: `ApiKeyServiceTests` (3), `WebhookTests` (5).
- Validated: `dotnet build` (0 errors), `dotnet test` — 131/131 passing (115 prior + 16 new),

  `npm run build` (frontend, clean).

---

# Phase 21 — Public Marketplace, Online Payments, Advanced Recommendation Engine, Mobile App

Explicitly requested by the user (previously listed under "Later" as out-of-scope). All four close out the roadmap — nothing remains planned.

### Public Marketplace

- `Unit.IsPubliclyListed` (opt-in per unit, off by default) — the only field an agent/admin

  toggles to expose a unit outside their own tenant.

- `IMarketplaceService`/`MarketplaceService`: the app's one deliberately unauthenticated,

  cross-tenant read surface — uses the existing `ForAllTenants<T>()` escape hatch (previously SuperAdmin-only) to list Available + IsPubliclyListed units across every company, projected into `PublicUnitDto` (no CompanyId, no internal ids, no financial terms beyond price).

- API: `MarketplaceController` (`GET /api/marketplace/units`, no auth), rate-limited by IP

  (30 req/min — tighter than the API-key surface since there's no revocable credential to fall back on).

- Frontend: `/marketplace` — a standalone public page (outside `MainLayout`/`ProtectedRoute`),

  premium hero + filterable grid, no login required. `UnitForm` gained a "List on the public marketplace" checkbox.

- Tests: `MarketplaceServiceTests` (2).

### Online Payments

- `IPaymentGateway` abstraction, mirroring the `IEmailSender`/`IWhatsAppSender` pattern:

  `StripePaymentGateway` (real Stripe Checkout + webhook signature verification, keys read from `Stripe:*` configuration — never hardcoded) is registered only when `Stripe:SecretKey` is configured; otherwise a `NoOpPaymentGateway` logs instead of charging anyone, so the flow is wired end-to-end without a real Stripe account.

- `Payment` entity + `IPaymentService`: creates a Pending payment and a Stripe Checkout session

  for a Deal's down payment (defaults to the unit's `DownPayment`), and a webhook endpoint (`POST /api/payments/webhook`, unauthenticated by design — secured by HMAC signature verification instead) that marks it Paid/Failed.

- API: `PaymentsController` (`/api/deals/{dealId}/payments`, JWT auth) + `PaymentWebhooksController`.

  Frontend: "Collect Payment" action on Reserved deals (opens Stripe Checkout) and a payment history modal on the Deals page.

- Migration: `AddMarketplaceAndPayments` (bundled with the marketplace's `IsPubliclyListed`

  column). Tests: `PaymentServiceTests` (4).

- `.env.example`/`docker-compose.yml`/`appsettings.json` gained `Stripe:SecretKey`,

  `Stripe:WebhookSecret`, `Stripe:PublishableKey`, `App:PublicUrl` — all blank by default.

### Advanced Recommendation Engine

- `MlConversionScorer`: a real ML.NET (SDCA logistic regression) binary classifier, trained

  per-company from that company's own historical Contracted-vs-Cancelled deals, predicting conversion likelihood for a Lead/Unit pair. Requires at least 10 resolved deals to train — below that it returns null and `RecommendationService` falls back to pure rule-based scoring unchanged, so this never degrades the Phase 19 baseline. Trained models are cached in-process per company for 15 minutes.

- `RecommendationService` blends the two: 60% rule-based score + 40% ML conversion likelihood

  once a model is available; `UnitRecommendationDto.ConversionLikelihood` surfaces the raw probability. Frontend: shown as a small "X% predicted conversion (ML)" line on the Recommended Units panel when present.

- Training reads `Deal.FeatureSnapshotBudgetFit/LocationMatch/PropertyTypeMatch/PriceToBudgetRatio`

  — four columns `DealService.CreateAsync` populates once, at deal-creation time, via the shared `LeadUnitFeatureCalculator` (also used to score live candidates). This replaced an earlier version that joined the Lead's/Unit's *current* state instead, which drifted from what was actually true when the deal was made. Deals created before this column existed have a null snapshot and are correctly excluded from training — nothing to backfill, since that historical state isn't recoverable. Migration: `AddDealFeatureSnapshots`.

- Tests: `MlConversionScorerTests` (4, ~35s — real model training, not mocked; includes a

  regression test proving deals without a snapshot are excluded from the training set).

### Mobile App

- `mobile/` — a new Expo (React Native + TypeScript) project, independent of the `.NET`

  solution and the `client/` web app. Same auth as the web app (`/api/auth/login`, JWT bearer, tokens in `expo-secure-store`, auto-refresh-on-401 mirroring `client/.../api/client.ts`), consuming the Phase 20 Public API (`/api/v1`) for data — same roles/permissions/tenant isolation as everywhere else, no new backend surface needed.

- Screens: Login, Dashboard (KPI cards, pull-to-refresh), Leads (searchable list + detail with

  tap-to-call/tap-to-WhatsApp via native `Linking` — a capability the web app can't offer), Deals (read-only list). Read-only beyond login/browsing in this pass — write flows (`POST`/`PUT /api/v1/leads` already exist) are a natural next step.

- Design tokens mirror `client/.../index.css` (`mobile/src/theme/colors.ts`) so it reads as

  the same product.

- Validated via `npx tsc --noEmit` only at first — this environment has no iOS/Android SDK or

  emulator, so the app could not be run on a simulator/device here. Strengthened in Phase 22 (below) to the fullest automated validation possible without one. See `mobile/README.md`. **Superseded in Phase 23**: this Expo/React Native implementation was fully replaced by a Flutter rewrite at the user's explicit request. `mobile/` is now the Flutter project; no Expo/React Native code remains in the repository. See Phase 23 below.

- Validated: `dotnet build` (0 errors), `dotnet test` — 139/139 passing (131 prior + 8 new),

  `npm run build` (frontend, clean), `npx tsc --noEmit` (mobile, clean).

---

# Phase 22 — Documentation & Validation Completeness Pass

A full audit of every `.md` file in the repo for TODO/pending/deferred/limitation language, closing each item that was technically closeable and strengthening what wasn't.

### Mobile app validation (the explicit focus of this pass)

- Device/simulator execution remains genuinely impossible here — confirmed again: no

  `ANDROID_HOME`, no `adb`/`emulator` on PATH, no Android SDK, and no macOS/Xcode available for iOS (this is a Windows sandbox). This has not changed and cannot be worked around from inside the app's own repository.

- Everything short of that is now in place and passing:
  - `npx expo-doctor` — 20/20 checks passed.
  - ESLint (`eslint-config-expo`) — `npm run lint`, 0 errors.
  - A real Jest suite (`jest-expo` + `@testing-library/react-native`) — `npm test`, 9 tests:

    `jwtDecode` (payload decoding, array claims, unicode, malformed-token error path), `getApiErrorMessage`/`onSessionExpired` (client.ts), and a `StatusPill` component render test. (`@testing-library/react-native`'s `render()` is async under React 19 — every test awaits it.)

  - **Production bundle export** (`expo export --platform android` / `--platform ios`) — the

    strongest automated check available without a device: runs the full Metro bundler across the entire dependency graph (navigation, every screen, native modules, assets) and compiles to Hermes bytecode. Both platforms bundle cleanly (~2800 modules, no resolution errors). `npm run validate` runs typecheck + lint + tests + both platform exports in one command; `npm run export:check` isolates just the export step.

- What still needs a real device/simulator (unavoidable, listed precisely so nothing is

  silently assumed): visual/layout QA, touch interaction, `tel:`/`wa.me` deep links actually opening the Phone/WhatsApp apps, `expo-secure-store` on real iOS Keychain/Android Keystore, and push notification / background behavior (none implemented yet, but would need this too). `mobile/README.md` states this explicitly as the remaining manual QA step.

### Hangfire dashboard authorization (was: "restricted to local requests only... add a real

### authorization filter before deploying anywhere network-reachable" — docs/deployment.md)

- `HangfireDashboardAuthorizationFilter` (`Infrastructure/Auth`): HTTP Basic Auth against

  `Hangfire:DashboardUsername`/`DashboardPassword` (constant-time comparison via `CryptographicOperations.FixedTimeEquals`) when configured; falls back to the same local-requests-only behavior as before when left unset (safe default for local dev). The core check is a pure `AuthorizeHttpContext(HttpContext)` method, unit-testable without standing up a real Hangfire `DashboardContext`.

- `.env.example`/`docker-compose.yml`/`appsettings.json` gained `Hangfire:DashboardUsername`/

  `DashboardPassword`, both blank by default.

- Tests: `HangfireDashboardAuthorizationFilterTests` (6): loopback-allowed/denied when

  unconfigured, correct/wrong Basic Auth credentials, missing header once configured (proves the local-only fallback does NOT apply once credentials are set), malformed header.

### CI/CD automatic migrations (was: "No automatic migration-on-deploy step" — docs/deployment.md)

- `.github/workflows/azure-deploy.yml` gained a `migrate` job (`dotnet ef database update`

  against `secrets.AZURE_SQL_CONNECTION_STRING`) that runs after the images are built and before `deploy`, so the schema is always ready before traffic can reach code that expects it. If the secret isn't set, `migrate` is skipped (not failed) and `deploy` still proceeds — falling back to the documented manual step, not silently doing nothing.

### Advanced Recommendation Engine — deal-time feature snapshots (was: "known scope

### limitation... training features are built from current state, not a snapshot at deal time")

- `Deal` gained four nullable snapshot columns (`FeatureSnapshotBudgetFit/LocationMatch/`

  `PropertyTypeMatch/PriceToBudgetRatio`), populated once by `DealService.CreateAsync` via a new shared `LeadUnitFeatureCalculator` (extracted from what was `MlConversionScorer`'s private `BuildFeatures`, now used by both call sites so they can't drift apart). `MlConversionScorer` now trains directly from these snapshot columns instead of joining the Lead's/Unit's current state — the scope limitation is resolved, not just reworded. Migration: `AddDealFeatureSnapshots`. See the Phase 21 entry above for the full picture.

### Docker image builds — re-checked, unchanged (was: "Docker daemon was not running in this

### environment" — Phase 18)

- Re-attempted in this pass: `docker version` succeeds (the Docker CLI is present) but

  `docker compose config` needed a `.env` to parse at all, and starting Docker Desktop's own process did not bring up a responding daemon within a reasonable wait — the same environmental constraint as Phase 18, not a regression or a new finding. This sandboxed session has no working container runtime (Hyper-V/WSL2-backed Docker Desktop requires interactive first-run setup this environment can't drive). `docker compose config` (syntax only) still parses cleanly with all newly-added env vars resolved correctly — verified again in Phase 22 after the Stripe/Hangfire/App config additions.

- Validated: `dotnet build` (0 errors), `dotnet test` — 146/146 passing (139 prior + 7 new: 6

  Hangfire filter, 1 ML snapshot-exclusion regression), `npm run build` (frontend, clean), `mobile`: `npm run validate` (typecheck + lint + 9 tests + Android/iOS production export), all clean.

---

# Phase 23 — Flutter Mobile Migration & Web Design System Overhaul

Explicitly requested by the user: replace the Phase 21 Expo/React Native mobile app with a Flutter rewrite, and substantially redesign the web frontend's visual/UX system (toning down the gradient/glassmorphism-heavy look from the earlier redesign pass in favor of a more restrained, professional SaaS aesthetic).

### Flutter mobile migration (complete replacement, not a fork)

- Full feature parity with the Phase 21 Expo app: Login, Dashboard (KPI grid), Leads

  (debounced search + detail with tap-to-call/tap-to-WhatsApp), Deals (read-only list). Nothing regressed — every screen the RN app had, the Flutter app has.

- Clean, feature-based architecture: `domain → data → application → presentation` per feature

  (`auth`, `dashboard`, `leads`, `deals`), plus `core/` (network, storage, router, theme, connectivity) and `shared/` (reusable widgets, utils). See `mobile/README.md` for the full layout.

- **State management**: Riverpod (`flutter_riverpod`) — no code generation, providers

  colocated with their feature.

- **Networking**: a centralized `ApiClient` (Dio) — injects the Bearer token, de-duplicates

  concurrent 401-triggered refreshes into one refresh call, retries the original request, calls `onSessionExpired` on refresh failure, and maps every failure to a typed `ApiException` (`message`, `statusCode`, `isNetworkError`) before it reaches a screen.

- **Auth/storage**: `flutter_secure_storage` (iOS Keychain/Android Keystore) behind a

  `TokenStorage` interface with an in-memory test double — `AuthController` (`StateNotifier<AuthState>`, three states: unknown/authenticated/unauthenticated) owns the login/logout/session-expiry lifecycle, same contract as the web app's `AuthContext`.

- **Route protection**: `go_router` with a `redirect` callback extracted as a pure, unit-tested

  function (`computeAuthRedirect`) — unauthenticated users are bounced to `/login` from any other route; an authenticated user on `/login` is bounced to `/dashboard`; no redirect while auth status is still being resolved from storage, so a logged-in user never flashes the login screen.

- **Loading/empty/error/offline states**: `AsyncValueView<T>` renders any Riverpod

  `AsyncValue` uniformly; `ErrorView` distinguishes a real API error from a network/timeout failure (different icon/copy, always a Retry button); `EmptyView` gives each list its own contextual copy; `OfflineBanner` (via `connectivity_plus`) is a persistent banner shown whenever the device has no connectivity, independent of any specific failed request; every list/dashboard screen has pull-to-refresh.

- **Form validation**: `LoginScreen` validates required fields and email format client-side,

  surfaces server errors inline, and has a password-visibility toggle.

- **Theming**: light/dark `ThemeData` following `ThemeMode.system`, using the same toned-down

  color tokens as the redesigned web app (see below) so the product reads as one system.

- **RTL readiness**: no Arabic/Hebrew translations were added (UI language stays English, same

  as web), but the app avoids hardcoded left/right layout in favor of Flutter's inherently direction-aware Material widgets — adding a RTL locale later is a localization task, not a layout rewrite.

- Tests: 35 total — unit (`JwtDecoder`, `mapDioException`, `computeAuthRedirect`),

  service/repository tests against a fake `HttpClientAdapter` (`ApiClient`'s auth/refresh/ retry/dedup logic — including a real concurrent-401 dedup test — and `LeadsRepository`), state tests (`AuthController`, mocked repository via `mocktail`), and widget tests (`LoginScreen` validation, `StatusChip`, app-boots-to-login).

- Validated: `flutter pub get` (clean), `dart format .` (clean), `flutter analyze` (**0**

  errors/warnings, 2 optional info-level style hints), `flutter test` (**35/35 passing**), `flutter build web --release` (**succeeds** — a full production compile of the entire app, ~2800 modules, used as the strongest available substitute for a device build), `flutter build apk --debug` (**fails**: `No Android SDK found` — this sandboxed environment has no Android SDK/emulator, confirmed by direct inspection; not a code defect), iOS build not attempted (`flutter build ios` requires Xcode/macOS; this environment is Windows, structurally impossible here regardless of SDK setup).

- Old Expo/React Native project fully removed (`mobile/node_modules`, `mobile/src`,

  `mobile/.expo`, `package.json`, etc.) — the Flutter project now occupies `mobile/`. No two competing mobile implementations remain.

### Web design system overhaul

- Toned down the gradient/glassmorphism-heavy look introduced in the earlier premium-redesign

  pass, per explicit new direction: fewer/no decorative gradients on interactive elements, no navbar backdrop blur, a solid (not animated/particle) login panel, a tighter border-radius scale — restrained, professional SaaS aesthetic rather than a marketing-site look. Design tokens, status-color system, tables/forms/badges/skeletons kept — those were already solid — the change is concentrated in the chrome (sidebar/navbar/login) and interactive element styling (buttons/avatars/progress bars) that leaned hardest on gradients before.

- Validated: `dotnet build`/`dotnet test` re-run unaffected (pure frontend change) —

  `npm run build` (frontend, clean), `flutter analyze`/`flutter test` unaffected (color tokens only mirrored, not shared code).

---

# Phase 24 — Documentation Audit Remediation

A full `.md`-file audit (TODO/pending/deferred/stub/limitation language, cross-checked against the actual code) found several genuinely actionable gaps. All were closed in this phase:

- **Real email delivery**: `SmtpEmailSender` (System.Net.Mail, no new package) is now

  registered whenever `Smtp:Host` is configured — mirrors the existing Stripe configured-vs-not-configured DI pattern. Falls back to `LoggingEmailSender` when unset. Never throws into the caller (a delivery failure must not surface an SMTP stack trace or leak account-existence info via a forgot-password error). `.env.example`/`docker-compose.yml`/ `appsettings.json` gained `Smtp:*` keys, blank by default. Tests: `SmtpEmailSenderTests` (1).

- **Real WhatsApp delivery**: `WhatsAppCloudApiSender` (plain `HttpClient` against Meta's

  WhatsApp Business Cloud API — no vendor SDK needed) is now registered whenever both `WhatsApp:PhoneNumberId` and `WhatsApp:AccessToken` are configured; falls back to `LoggingWhatsAppSender` otherwise. This closes a real gap the Phase 19 note understated: no real-send code path existed at all before this, unlike the Stripe/NoOp pattern it was meant to mirror. `.env.example`/`docker-compose.yml`/`appsettings.json` gained `WhatsApp:*` keys, blank by default. Tests: `WhatsAppCloudApiSenderTests` (3, against a fake `HttpMessageHandler` — success, non-2xx response, and network-exception paths).

- **CI now gates deploys on tests**: `.github/workflows/azure-deploy.yml` gained a `test` job

  (`dotnet build`/`dotnet test`, `npm ci`/`lint`/`build`) that every other job now depends on (`build-and-push: needs: test`) — a failing build or test suite blocks the deploy entirely instead of reaching production untested.

- **Documentation corrections**:
  - `docs/decisions.md` "Not Initial Scope" section corrected — it still listed 8 features

    (AI Assistant, WhatsApp, mobile, marketplace, billing, payments, marketing automation, external API) as "do not implement," directly contradicting the shipped product. Now struck through with pointers to the phase that implemented each.

  - `docs/api.md` gained an "Other Internal Endpoints" section covering the ~16 controllers

    (Users, Tasks, Commissions, Reports, Audit Logs, Documents, Subscriptions, WhatsApp, Campaigns, API Keys, Webhooks, Marketplace, Payments, image uploads) it previously didn't document at all — it only ever covered the Phase 4–6 core CRUD modules.

  - `docs/database.md` gained a "Later-Phase Entities" section documenting the ~13 entities

    added since Phase 12 (`SubscriptionPlan`, `CompanySubscription`, `WhatsAppTemplate`, `WhatsAppMessage`, `Campaign`, `CampaignRecipient`, `ApiKey`, `WebhookSubscription`, `WebhookDelivery`, `Payment`, `ProjectImage`, `UnitImage`, `Document`) plus the `Unit.IsPubliclyListed` and `Deal.FeatureSnapshot*` columns added later still.

  - `docs/frontend.md` gained a "Later-Phase Pages" section listing the 6 pages

    (Billing, WhatsApp Templates, Marketing Campaigns, API Keys, Webhooks, Marketplace) added after the original "Initial Pages" list — verified against `AppRoutes.tsx`, not assumed.

  - `docs/deployment.md` gained an "Email and WhatsApp delivery" section documenting the new

    config keys and the WhatsApp Cloud API's 24-hour customer-service-window limitation.

  - `client/real-estate-crm-react/README.md` was the unedited Vite scaffold template (zero

    project-specific content) — replaced with real run/build/structure instructions pointing at `docs/frontend.md`/`docs/architecture.md`.

- Not changed (re-confirmed as genuinely non-actionable, not re-attempted): Android/iOS device

  builds (no SDK/Xcode in this sandbox), Docker Compose end-to-end startup (no working daemon here), real Azure/Stripe/WhatsApp Cloud API credentials (this repository cannot provision or obtain third-party accounts on the user's behalf), and RTL translation strings (a content task, not a code task).

- Validated: `dotnet build` (0 errors), `dotnet test` — **150/150 passing** (146 prior + 4 new:

  1 SMTP, 3 WhatsApp Cloud API), `npm run lint` (frontend, clean — pre-existing warnings only), `npm run build` (frontend, clean, 749.81 kB). Flutter and CI YAML were not modified beyond the `azure-deploy.yml` test gate, which cannot be executed here (no GitHub Actions runner in this sandbox) — reasoned through statically and matches the existing job's proven structure.

---

# Phase 25 — Production-Readiness & QA Pass

A full production-readiness/QA sweep across backend, web, Flutter, CI/CD, and configuration — auth, multi-tenancy, CRUD, validation, background jobs, integrations, payments, webhooks, API keys, migrations, concurrency, logging, secrets, rate limiting, security headers, frontend states, responsiveness, accessibility, Flutter navigation/offline handling, and performance. Real defects were found and fixed; nothing was marked "production-ready" until Critical/High items were closed.

### Critical

- **Android release build had no INTERNET permission**: `android/app/src/main/AndroidManifest.xml`

  was missing `android.permission.INTERNET` — only the debug/profile manifest overlays had it (Flutter adds those automatically for hot-reload). A release APK would have been unable to make any network request at all — the entire app is API-driven, so this made the shipped app completely non-functional. Fixed by adding the permission to the main manifest. Found by direct manifest inspection (the only Android release-build check possible without a device/SDK here).

### High

- **No rate limiting on auth endpoints**: `login`/`refresh`/`logout`/`forgot-password`/

  `reset-password` had zero rate limiting — unlimited credential-stuffing/brute-force and forgot-password email-bombing were possible. Added an `Auth` rate-limit policy (10 req/min per IP — these are all `[AllowAnonymous]`, so IP is the only available key) applied via `[EnableRateLimiting("Auth")]` on `AuthController`.

- **Password change/reset didn't revoke other sessions' refresh tokens**: a stolen refresh token

  survived a password change indefinitely. `ChangePasswordAsync`/`ResetPasswordAsync` now call a new `RevokeAllActiveRefreshTokensAsync` after a successful change — every other device/session is forced to log in again. `AuthService` had zero prior tests despite being the most security-critical service in the app; added `AuthServiceTests` (14 tests, against a real `UserManager` + in-memory EF DB, not mocked) covering login/refresh/logout/change/forgot/reset, including an explicit test that login gives an identical error for "wrong password" and "unknown email" (no account-enumeration oracle).

- **Unit double-booking race condition**: two concurrent `ReserveAsync` calls against the same

  unit could both read `Status == Available` before either wrote, both succeed, and silently double-book the unit to two different deals — a real data-integrity bug, not just a theoretical one. Fixed by marking `Unit.UpdatedAt` as an EF Core optimistic concurrency token (no schema/ migration change — pure EF metadata) and translating `DbUpdateConcurrencyException` to a 409 in `DealService` (`Reserve`/`Contract`/`Cancel`) and `UnitService.UpdateAsync`. Deterministic regression tests added (two independent `DbContext`s against the same in-memory database, simulating genuine concurrent reads) proving the race is closed, not just that the code compiles: `ReserveAsync_ThrowsConflict_WhenUnitWasConcurrentlyModified`, `UpdateAsync_ThrowsConflict_NotDuplicateCodeMessage_WhenConcurrentlyModified`. The second test also caught a **real bug the first fix introduced**: `DbUpdateConcurrencyException` derives from `DbUpdateException`, so `UnitService`'s existing catch-all for duplicate unit codes was silently swallowing concurrency conflicts too and mislabeling them "A unit with this code already exists" — fixed by ordering the `DbUpdateConcurrencyException` catch first.

- **`Microsoft.OpenApi` 2.0.0 known high-severity vulnerability** (GHSA-v5pm-xwqc-g5wc, stack

  overflow parsing a circular-reference OpenAPI document): a transitive dependency of `Microsoft.AspNetCore.OpenApi`. Pinned a direct `Microsoft.OpenApi 2.7.5` reference (patched version) to override it — `dotnet build` no longer reports the NU1903 warning.

- **Web app had zero automated tests**: only ESLint/`tsc` build checks existed for the entire

  React SPA — no unit or component test had ever been written. Added Vitest + `@testing-library/react` (`npm test`) and 19 tests covering the highest-risk untested logic: `apiClient`'s 401→refresh→retry flow and concurrent-refresh deduplication (stubbing axios' adapter function directly, no network), `getApiErrorMessage`, `tokenStorage`, and the new `Modal` focus-trap/restore-focus behavior below. `azure-deploy.yml`'s `test` job now runs `npm test` too.

### Medium

- **No security-response headers**: no middleware set `X-Content-Type-Options`,

  `X-Frame-Options`, `Referrer-Policy`, or `Permissions-Policy` on any response, and `UseHsts()` was never called outside Development. Added a small header middleware plus conditional `UseHsts()` in `Program.cs`.

- **Modal had no keyboard focus trap**: `Tab`/`Shift+Tab` walked straight through every modal

  (including the shared `ConfirmDialog`) into page content behind the overlay — a real keyboard- navigation dead end and a WCAG 2.4.3 violation. `Modal.tsx` now traps focus, moves initial focus into the dialog on open, and restores it to the triggering element on close. Covered by 6 new `Modal.test.tsx` tests (escape-to-close, overlay-click-to-close vs. content-click-does-not, initial focus, Tab-wrap trap, focus restoration).

- **One large frontend bundle**: every page was a static import, so the first load shipped one

  ~750 kB JS bundle regardless of which page a user actually visited. `AppRoutes.tsx` now lazy- loads every page component (`React.lazy` + `Suspense`); the shared vendor chunk (React, TanStack Query, etc.) is ~644 kB and loads once, but each page is now its own 1.6–10 kB chunk fetched only on navigation to it.

- **No test/build gate on the deploy pipeline for Flutter**: `azure-deploy.yml`'s `test` job only

  covered backend/web. Added a `test-mobile` job (`flutter pub get`, `dart format --set-exit-if-changed`, `flutter analyze`, `flutter test`) that `build-and-push` now also depends on. Running these locally caught a real (if cosmetic) issue: the installed Dart SDK's formatter now wraps long parameter lists differently than when the mobile files were last formatted — re-ran `dart format .` (10 files, whitespace-only) so the CI check actually passes.

- **go_router had no error/unknown-route screen**: an unmatched route fell through to go_router's

  default raw exception-dump screen. Added an `errorBuilder` with a normal "Not found" screen and a way back to the dashboard.

### Low / verified-clean (no action needed)

- Pagination page-size caps are applied consistently across all 8 paginated list endpoints

  (Leads/Projects/Units/Deals/Tasks/Commissions/AuditLogs capped at 100, Marketplace at 50) — checked every service directly rather than assuming.

- N+1 query risk: spot-checked `ReportsService`, `LeadService`, `RecommendationService` — all use

  server-side grouping/single queries with `AsNoTracking()` on read paths; no per-row query loops found.

- Stripe webhook signature verification is real (`EventUtility.ConstructEvent` against

  `Stripe:WebhookSecret`) — not a gap, verified by reading `StripePaymentGateway` directly.

- CORS is allow-listed (never `AllowAnyOrigin`); the web app's axios client already deduplicates

  concurrent token refreshes correctly (this was the reference implementation the new Vitest tests now lock in).

- No `dangerouslySetInnerHTML`/`eval` in the web app — reduces (but doesn't eliminate) the

  real-world exploitability of storing JWTs in `localStorage` (see Residual risks below).

- EF Core migrations are in sync with the model (`dotnet ef migrations has-pending-model-changes`

  reports none) — the concurrency-token change needed no migration.

### Residual risks (not resolved inside this repository)

- **JWTs/refresh tokens stored in `localStorage`**, not an `httpOnly` cookie — an XSS

  vulnerability anywhere in the app (or a compromised dependency) could exfiltrate tokens. Switching to cookie-based auth is a real architecture change (backend `Set-Cookie` support, CSRF tokens, `SameSite`/CORS-credentials reconfiguration) — out of scope for a defect-fixing pass; flagged here rather than silently accepted. Partially mitigated: no `dangerouslySetInnerHTML`/`eval` usage found anywhere in the current codebase.

- **No per-account lockout**, only IP-based rate limiting — `AuthService.LoginAsync` calls

  `UserManager.CheckPasswordAsync` directly (not `SignInManager`'s lockout-tracking path). Deliberately not added in this pass: a naive implementation risks becoming an account-enumeration side channel (a locked account would need to respond differently from a wrong-password one), which would undercut the account-enumeration protection just added/tested in `ForgotPasswordAsync`/`LoginAsync`. Would need careful design, not a quick addition.

- Real device/simulator, live browser, Docker, cloud infra, and third-party credential validation

  remain exactly as scoped in Phases 18/22/23: none of those are available inside this sandbox.

- Validated: `dotnet build` (0 errors, 0 warnings — the OpenAPI vulnerability warning is gone),

  `dotnet test` — **167/167 passing** (150 at the end of Phase 24 + 14 new `AuthServiceTests` + 3 new concurrency-regression tests), `npm run lint` (frontend, clean), `npm test` (frontend, **19/19 passing**, new test suite), `npm run build` (frontend, clean, per-page chunks now ~1.6–10 kB each instead of one 750 kB bundle), `dart format`/`flutter analyze`/`flutter test` (mobile, clean, **35/35 passing**).

---

# Phase 26 — Web Token-Storage Security Assessment (Cookie Migration)

A final, explicit assessment of whether the web SPA should move off `localStorage` for auth-token storage (the residual risk flagged at the end of Phase 25), and implementation of the result. Verdict: **practical for the web client only**, implemented as a fully additive change that does not touch Flutter or the Public API's authentication in any way. Full design rationale in `docs/auth.md#web-cookie-auth` — summary:

- Refresh token: moved out of `localStorage` entirely, into a `Secure` (non-Development),

  `HttpOnly`, `SameSite=None`/`Lax` (per environment) cookie scoped to `/api/auth`. It is now unreachable to any web JS at all — including an XSS payload that hooks `fetch`/`XHR` to read response bodies directly, not just one that reads storage — because the server blanks `AuthResponse.RefreshToken` in the JSON body whenever a request opts into cookie transport.

- Access token: moved out of `localStorage` into an in-memory-only module variable

  (`utils/authSession.ts`). Lost on page reload by design; `AuthProvider` silently re-fetches one on mount via the refresh cookie, same pattern as a native app's "check for a stored session on launch."

- CSRF: a double-submit `XSRF-TOKEN` cookie (non-HttpOnly, JS-readable) + `X-CSRF-Token` header,

  constant-time-compared server-side, required on any cookie-mode `refresh` call.

- **Purely additive/opt-in**: a caller must send `X-Auth-Transport: cookie` (web only) to get

  any of this; Flutter and every Public API/third-party integration keep using the exact same JSON-body-token flow as always, unmodified, unaffected. `AuthService`'s core logic (login/ refresh/logout/password change) was not touched — only `AuthController` gained the transport- selection logic, in a new small `WebAuthCookies` helper.

- Explicitly scoped out (see `docs/auth.md`'s "Why not Flutter or the Public API"): neither of

  the other two clients has the browser-JS-execution-context threat model cookies defend against, so moving them would add real complexity for no security benefit.

- Tests: `WebAuthCookiesTests` (12, direct `DefaultHttpContext` — CSRF generation/comparison,

  cookie flags per environment, path scoping, clearing). `RealEstateCRM.Tests` gained a project reference to `RealEstateCRM.Api` to reach the new type. Frontend: `client.test.ts` rewritten for the cookie flow (refresh-call headers/credentials, dedup, session-expiry — same behaviors as before, different transport), `tokenStorage.test.ts` replaced by `authSession.test.ts` (in-memory-only access token, CSRF-cookie parsing, proves nothing is written to `localStorage`).

- Validated: `dotnet build` (0 errors), `dotnet test` — **179/179 passing** (167 prior + 12 new),

  `npm run lint` (frontend, clean), `npm test` (frontend, **24/24 passing**), `npm run build` (frontend, clean). `AuthServiceTests` from Phase 25 required no changes — `AuthService`'s public contract didn't change.

- Not validated here (requires a real browser): actual cross-origin cookie delivery in a live

  two-origin deployment (dev-tools/manual verification that `Set-Cookie`/`Cookie` round-trip correctly, that `SameSite=None;Secure` cookies survive a real HTTPS deployment, and that the CSRF header is attached correctly by a running browser instance) — the `DefaultHttpContext` tests above prove the header/cookie *logic* is correct in isolation; they cannot prove a real browser's cookie jar behaves as assumed end-to-end. Flag this as the one remaining manual check before relying on this in production.

---

# Phase 27 — Live Local Run & Visual QA

Ran the whole stack for real for the first time in this project's history — backend against a real SQL Server (LocalDB) and real Redis (not the in-memory EF provider every prior test used), web frontend against it, Flutter against it as a web build — logged in with seeded realistic data and clicked/inspected through the running app instead of relying on build/test success. This surfaced real defects no prior `dotnet test`/`npm test`/`flutter test` run had ever exercised, because none of them go through real HTTP JSON serialization or a real browser.

### Critical bugs found only by running the real stack (all fixed, verified live, tests added)

- **Every enum in every API response serialized as a raw integer, not a string** — no

  `JsonStringEnumConverter` was ever registered. `dotnet test` never caught this because it asserts against DTOs directly, never through real JSON serialization. Broke `StatusBadge` (and every raw `{entity.status}` interpolation) everywhere: Leads, Units, Deals, Commissions, Tasks, Subscriptions, WhatsApp messages, Campaigns — showing plain numbers instead of labeled, colored badges app-wide. Fixed with one line in `Program.cs` (`AddJsonOptions(...JsonStringEnumConverter())`), verified via `curl` and live in-browser on Leads/Units/Deals.

- **The JWT's role claim used .NET's long `ClaimTypes.Role` URI instead of the short `"role"`

  name documented in `docs/auth.md`** — the frontend's client-side JWT decode (which reads the token's raw payload keys directly, with none of ASP.NET Core's inbound claim-type mapping) could never find a `role` property, so `user.roles` has been an empty array for every browser session, always. This silently hid all 8 role-gated nav items/routes (Billing, Users, Company Settings, Commissions, WhatsApp Templates, Marketing Campaigns, API Keys, Webhooks) from every user, regardless of actual role — the single most severe bug found this whole engagement, and one that no unit/integration test had ever been positioned to catch (they assert against `ClaimsPrincipal`/`AuthResponse` objects, never a real client-side JWT decode of the wire-format token). Fixed in `JwtTokenGenerator.cs` by switching to the short `"role"` claim type; a new test (`GenerateAccessToken_RoleClaim_StillResolvesToClaimTypesRole_...`) proves `[Authorize(Roles=...)]` still works server-side by running a real token through the real validation pipeline, not just an assumption about .NET's default inbound claim map. Verified live: all 8 previously-hidden nav items now render and their pages load real data.

### Other real bugs found the same way

- Dashboard KPI numbers used a `requestAnimationFrame` count-up animation with no fallback —

  stuck at 0 forever in a non-compositing/backgrounded browser tab (confirmed by waiting 3+ seconds with no progress). Fixed with a `prefers-reduced-motion` check (this also wasn't being respected before — an accessibility gap on its own) and a `setTimeout` correctness fallback that fires even when `requestAnimationFrame` is throttled/paused (`components/StatCard.tsx`).

- The navbar rendered the raw user GUID instead of a name — the JWT never carried one. Added a

  `name` claim (`JwtTokenGenerator.cs`, `AuthService.cs`) rather than a new "current user" endpoint (none existed, and `GET /api/users` is CompanyAdmin/SuperAdmin-only, so a SalesAgent couldn't have looked up their own name that way either).

- Currency values in the Leads/Units/Deals tables were unformatted raw numbers (`690000`),

  inconsistent with the Dashboard/payment-history views that already used thousands separators. Added a shared `utils/format.ts#formatCurrency`.

- 10 different tables across the app had an empty, unlabeled `<th></th>` for their row-actions

  column — a real accessibility gap (no accessible name for that column). Added a `.sr-only` utility class and an "Actions" label to all 10.

### Environment used for this run (not part of the shipped app)

- SQL Server: LocalDB (`(localdb)\MSSQLLocalDB`), already present on this machine — migrations

  applied via the documented `dotnet ef database update` command.

- Redis: installed via `winget install Redis.Redis` (hash-verified, official

  microsoftarchive GitHub release) since Docker's daemon is unavailable in this sandbox (same constraint as every prior phase) and no Redis was otherwise present.

- A throwaway seed console project (outside the tracked solution, in the session scratchpad —

  never part of this repository) created one company, 4 users, 3 projects, 10 units, 10 leads, 3 deals, a commission, and tasks directly via the same `ApplicationDbContext`/`UserManager` the real app uses, so the data is exactly as realistic as data created through the UI.

- CORS temporarily allow-listed `http://127.0.0.1:8090` (the Flutter web-server's origin) via an

  environment variable for this session only — never written to any committed config. A real Flutter app (Android/iOS) has no such restriction at all; this was purely an artifact of testing Flutter's web target instead of a device/emulator.

### What could not be verified this pass, and exactly why

- **Pixel screenshots of any screen (web or Flutter)**: the embedded Browser pane tool failed

  to composite/render frames for the entire session (confirmed repeatedly, not transient), and the Claude-in-Chrome extension was not connected. An attempted OS-level screenshot fallback captured the operator's real live desktop instead of an isolated one — immediately deleted, not repeated. All verification this pass was done via DOM/accessibility-tree text extraction and network/console inspection instead, which is weaker than an actual visual screenshot for catching pure layout/spacing/color issues (though it did catch several real functional/data bugs a visual-only pass might have missed, like the role-claim and enum-serialization bugs).

- **Any interactive Flutter verification**: Flutter web renders entirely to one `<canvas>`

  (CanvasKit) with no DOM/accessibility tree until its semantics bootstrap is triggered by a genuine, browser-trusted click — synthetic JS-dispatched events do not trigger it (confirmed by direct testing), and no coordinate-based click is possible without a screenshot to establish the viewport first. Confirmed instead via console/network inspection only: the compiled app loads with no unexpected errors, correctly bounces an unauthenticated session to `/login`, and its API calls reach the real backend successfully once CORS was opened for its origin. A real device/emulator or a working screenshot surface is required to go further.

- Validated: `dotnet build` (0 errors), `dotnet test` — **180/180 passing** (179 prior + 1 new

  role-claim regression test), `npm run lint` (clean), `npm test` (**24/24 passing**), `npm run build` (clean), `flutter analyze`/`dart format` unaffected (no Flutter code changed this pass — confirmed clean, 0 errors/warnings). All fixes re-verified live in the running app after each change, not just via automated tests.

---

# Phase 28 — Dashboard Enhancements & Whole-Project Review

Two parts: the Dashboard gained the charts/activity-feed/KPI-polish the user asked for, then a full project + `.md` review swept for anything else missing — which found a real, previously unnoticed bug affecting money formatting across the app, closed it everywhere at once, and closed a self-introduced gap (zero tests) from the Dashboard work itself.

### Dashboard: pipeline chart, recent activity, KPI polish

- **Leads Pipeline chart** (`LeadsPipelineChart.tsx`) — a `recharts` bar chart (the package was

  a declared dependency for phases but had never actually been used anywhere until now) showing leads by status in pipeline order, colored via the same `statusVariant` mapping `StatusBadge` uses everywhere else. Includes a `role="img"`/`aria-label` text summary so screen readers get the same information sighted users get from the bars, not a silently-skipped SVG.

- **Recent Activity panel** (`RecentActivity.tsx`) — merges the newest leads and deals (via the

  existing list endpoints, sorted by `createdAt` — no new backend endpoint) into one chronological feed with relative timestamps ("2h ago") and links to each item's detail page.

- **KPI cards** — a slim top-accent bar per metric's category, and a `$` prefix on Total Sales

  Value.

- Tests: `RecentActivity.test.tsx` (11 — `timeAgo` formatting at every threshold, loading/error/

  empty states, merge-and-cap-at-6 ordering, link targets) and `LeadsPipelineChart.test.tsx` (3 — empty-state logic, not brittle SVG-internals assertions). These didn't exist when the components first shipped — added while reviewing "what's missing" per the user's own request, since shipping new dashboard logic with zero tests broke this project's own established pattern of testing real business logic, not just security-critical paths.

### Found while reviewing: a real, systemic money-formatting bug

- **`docs/frontend.md`'s Reports page KPI cards used a CSS class (`.kpi-card`) that was never

  defined anywhere** — a real, visible bug: those 4 cards rendered with zero card styling (no background/padding/radius/shadow), inconsistent with every other card in the app. Fixed by switching them to the same `StatCard` component the Dashboard uses (`ReportsPage.tsx`) — fixes the missing-CSS-class bug and the money-formatting bug in the same change.

- **7 more places showing raw unformatted numbers** (no thousands separator) for money values —

  the same defect class caught once already in Phase 27 (Leads/Units/Deals list tables), but missed elsewhere at the time: `CommissionsListPage.tsx` (commission amount, company commission), `CommissionForm.tsx`'s deal picker, `DealForm.tsx`'s unit picker, `UnitDetailsPage.tsx` (price, down payment), `LeadDetailsPage.tsx` (budget range), `ProjectsListPage.tsx` (starting price), and the Reports page's Agent Performance table (commission earned). All now use the shared `utils/format.ts#formatCurrency` from Phase 27. Verified `BillingPage.tsx` and `MarketplacePage.tsx` were already correct (already used `.toLocaleString()`) — not every money value in the app had this bug, only these 7 spots.

### `.md` review

- `docs/frontend.md`'s Dashboard section updated to describe the new chart/activity panels

  (previously only described the KPI grid).

- No other `.md` file needed changes — `docs/roadmap.md` (this entry), `docs/api.md`,

  `docs/database.md`, `docs/auth.md`, `docs/deployment.md`, `docs/decisions.md`, `docs/architecture.md`, `docs/multi-tenancy.md`, `docs/public-api.md`, both READMEs, and `CLAUDE.md` were all re-checked and remain accurate as of this phase.

### Confirmed not newly broken, and not otherwise pursued further this pass

- Flutter has no equivalent chart/activity-feed — intentional; mobile stays deliberately

  simpler/read-only per its own documented scope (`mobile/README.md`), not a gap.

- Most feature pages (Units, Deals, Billing, Webhooks, API Keys, etc.) still have no dedicated

  frontend tests beyond the security-critical paths tested in Phases 25–26 — an existing, already-disclosed scope decision, not something newly found.

- Residual risks already on record and unchanged: no per-account login lockout (Phase 25),

  JWTs/access-token-in-memory + refresh-cookie architecture is web-only by design (Phase 26), and every device/Docker/cloud-credential validation gap already disclosed in prior phases.

- Validated: `dotnet build` (0 errors), `dotnet test` — **180/180 passing** (unaffected, no

  backend changes this phase), `npm run lint` (clean), `npm test` — **37/37 passing** (24 prior + 13 new), `npm run build` (clean), `flutter analyze`/`flutter test` — unaffected, re-run anyway and confirmed clean (0 errors/warnings, **35/35 passing**).

---

# Phase 29 — Mecodex Brand Integration

`client/real-estate-crm-react/Mecodex-Brand-Assets/` (logo/icon/favicon SVGs+PNGs, brand colors) had been added to the repo but never wired into the running app — a real, user-visible gap found on review: the browser tab still showed the generic Vite favicon/title, and every user-facing surface (Sidebar, Login page, public Marketplace hero) hardcoded "Real Estate CRM" as the product name with no logo image anywhere.

- Copied the needed source assets into `public/` (`mecodex-favicon.svg`, `mecodex-icon.svg`,

  `favicon.ico`, `mecodex-favicon-192.png`, `mecodex-favicon-512.png`) — `Mecodex-Brand-Assets/` itself stays as the untouched source-of-truth folder, not referenced at runtime.

- `index.html`: favicon/apple-touch-icon now point at the Mecodex assets, `<title>` and meta

  description changed from "Real Estate CRM" to "Mecodex".

- `Sidebar.tsx` and `LoginPage.tsx`: the generic `Building` icon brand-mark replaced with the

  Mecodex icon SVG, label text changed to "Mecodex".

- `MarketplacePage.tsx`: public hero label changed from "Real Estate CRM Marketplace" to

  "Mecodex Marketplace" (icon kept — it's a generic building glyph there, not the brand mark).

- Not changed: the internal project name ("Real Estate CRM SaaS" in `CLAUDE.md`, the .NET

  solution/namespaces, `package.json` name) — this phase is a user-facing visual rebrand only, not a project rename, which would be a much larger and riskier change nobody asked for. Flutter (`mobile/`) was not touched — it has its own separate app icon/branding surface and wiring it up is a distinct task if wanted later.

- Validated: `npm run build` (frontend, clean).

### Flutter app icon/branding (follow-up, same phase)

The Flutter app (`mobile/`) still had 100% default Flutter-scaffold branding — app label "mobile"/"Mobile", the generated placeholder launcher icon, and the default Flutter-blue (`#0175C2`) web manifest — closed in the same pass:

- Generated a flat "icon on Ink Dark square" app icon (same convention as the web favicon,

  composited from `Mecodex-Brand-Assets/PNG/Icon/mecodex-icon-1024.png` onto `#0A0F1C`) via a PowerShell/`System.Drawing` script (no Flutter SDK, ImageMagick, or Python available in this sandbox to use `flutter_launcher_icons`) and resized it down, never up, to every required size: Android legacy launcher (`mipmap-mdpi/hdpi/xhdpi/xxhdpi/xxxhdpi`, 48–192px — no adaptive- icon XML exists, so a single flat PNG per density is correct), the full iOS `AppIcon.appiconset` (20–1024px per `Contents.json`, opaque as Apple requires), and the Flutter-web `icons/Icon-192/512.png` + extra-padded `Icon-maskable-192/512.png` + `favicon.png`.

- Android: `AndroidManifest.xml`'s `android:label` "mobile" → "Mecodex". iOS:

  `Info.plist`'s `CFBundleDisplayName`/`CFBundleName` "Mobile"/"mobile" → "Mecodex". App itself: `app.dart`'s `MaterialApp.router(title: ...)` "Real Estate CRM" → "Mecodex" (task-switcher label only — did not rename the `RealEstateCrmApp` Dart class, same reasoning as not renaming the .NET solution: internal identifiers aren't a user-facing branding surface).

- Flutter web (`mobile/web/`, used for the `flutter build web` verification path — see Phase 23):

  `manifest.json` name/short_name/description/background_color/theme_color and `index.html`'s title/description/apple-mobile-web-app-title/theme-color, all replaced from Flutter's unedited scaffold defaults.

- Not changed: Flutter's `AppTheme` color tokens (still the app's own restrained palette, not

  Mecodex teal/blue) — same scope decision as the web client's `--color-primary`, a bigger visual-system change nobody asked for, not a branding-integration gap.

- Validated: PNG dimensions spot-checked (`ic_launcher.png` 192×192 at xxxhdpi, maskable 512×512

  renders inside the safe zone), `Info.plist`/`AndroidManifest.xml`/`manifest.json` still well-formed after the edits (tag/key counts, JSON parse). **Follow-up, once the Flutter SDK became available in this sandbox**: `flutter pub get` (clean), `dart format --set-exit-if-changed .` (clean, 41 files), `flutter analyze` (**0** errors/warnings — same 2 pre-existing info-level style hints as Phase 23, in unrelated files), `flutter test` (**35/35 passing**, unchanged from Phase 23 — the branding edits touched no Dart logic), `flutter build web --release` (**succeeds**; the "Wasm dry run failed" output is an informational warning about `flutter_secure_storage_web`'s use of `dart:html`/`dart:js_util`, a pre-existing dependency limitation unrelated to this change, not a build failure). Verified directly in `build/web/index.html` and `build/web/manifest.json` that the Mecodex name/title/theme-color actually made it into the compiled output, not just the source files. Android APK/iOS builds still not attempted — no Android SDK/Xcode in this environment, same constraint as every prior phase.

### Launch/splash screen (found on review: both platforms flashed plain white on cold start)

  Neither platform's native splash screen had been touched since scaffolding — a real, visible gap the app-icon work alone didn't cover: the icon only appears once the Flutter engine draws its first frame, so every cold start briefly showed a plain white screen before the (dark) app UI, jarring against the rest of the now-dark-themed branding.

- Android: `drawable/launch_background.xml` and `drawable-v21/launch_background.xml`

  (light/dark — this app has no separate night-mode launch theme, both already pointed at the same drawable) now paint `#0A0F1C` instead of white/`?android:colorBackground`, with `@mipmap/ic_launcher` centered on top.

- iOS: `LaunchScreen.storyboard`'s background color changed from white to the same Ink Dark

  (`0.0392, 0.0588, 0.1098` sRGB = `#0A0F1C`), and the `LaunchImage` asset — previously three literal 1×1 transparent-pixel placeholders (Flutter's scaffold default, effectively invisible) — replaced with a real transparent Mecodex icon glyph at 84/168/252px (@1x/@2x/@3x).

- Validated: `flutter analyze` (0 errors/warnings, same 2 pre-existing hints), `flutter test`

  (35/35 passing) re-confirmed unaffected. Actual on-device splash-screen rendering could not be verified — no Android/iOS device or simulator in this sandbox, same constraint as everywhere else in this file.

---

# Later

Nothing remains. Every previously-out-of-scope "Later" item has now been explicitly requested and implemented (Phases 19–21). Add new items here only if the user again defers something they don't want built yet.

---

# Claude Code Rules

Only implement the explicitly requested/current task. Never automatically continue to the next phase. After completing a roadmap item:

1. Validate it.
2. Mark it complete.
3. Update Current Task if necessary.
4. Stop.

Do not mark tasks complete unless implementation exists and validation succeeds.
