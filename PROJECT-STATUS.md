# Project Status — RealEstateCRM

> Last updated: 2026-08-29. This file describes **this project only**. Every project in the
> workspace has its own status file; nothing here carries over to another.
>
> Detail lives in `docs/` — `roadmap.md` (the long history), `decisions.md`, `architecture.md`,
> `multi-tenancy.md`, `auth.md`, `frontend.md`, `database.md`, `api.md`, `public-api.md`,
> `deployment.md`. This file is the entry point, not a replacement.

---

## 1. Done and closed

Multi-tenant real-estate CRM: .NET backend, React/TypeScript web client, and a mobile client.

Recent, and previously undocumented outside the commit log:

- **Tenant isolation is enforced at build time.** A test **fails the build** when a tenant-scoped
  entity would go unfiltered. This is structural rather than convention-based — it does not rely on
  a developer remembering to add a filter.
- **Liveness and readiness health endpoints.**
- **Secrets validation** — the app refuses to start outside Development with unconfigured secrets.
- **CI gates on dependency vulnerabilities.**
- **Shared design system adopted**, with **Tailwind CSS 3.4 added alongside** the existing
  hand-written CSS rather than replacing it. Both are live: a rewrite of working, styled screens was
  not worth the regression risk.
- **Navy Corporate theme** — this product's identity over the shared token architecture.

---

## 2. Decisions adopted

| Decision | Status here |
|---|---|
| **Azure** as the primary deployment target | Not wired yet |
| **Azure Key Vault** for production secrets | Not wired yet. Today: secrets validation that refuses to start outside Development when unconfigured |
| **Redis** belongs here | One of the three products scoped for it (with PosFlow and Gym Manager). **Not yet added** |
| **App Insights (backend) + Sentry (frontend)** | Not installed yet |
| **Move DB seeding and Hangfire init out of startup** | **Not done — see below.** This is the one adopted decision with code still contradicting it |
| **Navy Corporate theme** | Done |
| **Tailwind alongside existing CSS, not replacing it** | Done. Deliberate: incremental adoption over a risky rewrite |
| **Design system is vendored, not linked** | The copy in `client/real-estate-crm-react/design-system/` is a vendored snapshot. Source of truth is `MeCodex/design-system`; do not hand-edit the copy |

---

## 3. Still open

### The startup work (adopted decision, not yet implemented)

`src/RealEstateCRM.Api/Program.cs` still does two things at application start:

```csharp
using (var scope = app.Services.CreateScope())
    await RoleSeeder.SeedRolesAsync(scope.ServiceProvider);   // line ~242

RecurringJob.AddOrUpdate<ReminderJobs>("lead-follow-up-reminders", …);  // line ~247
RecurringJob.AddOrUpdate<ReminderJobs>("task-reminders", …);           // line ~253
```

Why this matters: both run on **every boot of every instance**. Seeding on startup races when more
than one instance starts together, and job registration on startup means the app cannot be scaled
or restarted without re-registering schedules. The agreed end state is that seeding becomes an
explicit deployment step and job registration moves out of the request-serving host.

Not done in this cleanup pass on purpose — it is a behavioural change needing its own tests, not a
documentation fix.

### Everything else open

- **Azure deployment, Key Vault, Redis, Application Insights, Sentry** — none wired.
- **Hangfire dashboard credentials.** `Hangfire:DashboardUsername` / `DashboardPassword` must be set
  before deploying anywhere network-reachable. The authorization filter falls back if they are
  unset, which is fine locally and not fine in production.

---

## 4. Known issues / technical debt

- **Two styling systems are live at once** — Tailwind and hand-written CSS. That is the deliberate
  cost of incremental adoption, but it does mean two places to look when a style is wrong. The
  Tailwind preset maps `--mx-*` tokens onto Tailwind's scale so the two at least resolve to the
  same values.
- **The vendored design system can drift.** Nothing checks that the copy here still matches
  `MeCodex/design-system`. A local edit, or an upstream regeneration, would go unnoticed.
- **Frontend bundle is large** — the main chunk is ~449 kB (135 kB gzipped) and the dashboard
  another ~363 kB. It builds fine; it is simply not small.

---

## 5. Deliberately deferred

| Item | Why |
|---|---|
| **Moving seeding / Hangfire init out of startup** | Adopted and still open. It changes runtime behaviour and needs its own tests; doing it inside a documentation-and-cleanup pass is how a "safe cleanup" breaks a boot path |
| **Replacing the hand-written CSS with Tailwind** | The screens work and are styled. A wholesale rewrite is regression risk with no user-visible gain; Tailwind is used for new work instead |
| **Linking rather than vendoring the design system** | Would need shared-package infrastructure across separate repos. Vendoring is the honest simple option; the drift risk is recorded above instead of hidden |
| **Redis** | Agreed for this product, but it is a behavioural change needing its own verification |
