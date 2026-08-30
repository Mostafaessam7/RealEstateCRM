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
- **Deployment tasks moved out of startup (2026-08-29).** Role seeding and Hangfire recurring-job
  registration used to run inline at the end of `Program.cs`, on every boot of every instance.
  They are now an explicit step — `dotnet RealEstateCRM.Api.dll --init` — which runs the tasks and
  exits without listening. Development still runs them on startup, because one local instance
  cannot race itself and an extra required command is friction that gets worked around.
  - **The seeder is now safe under concurrency independently of where it runs from.** It was
    check-then-act: every instance observed "role missing", every instance inserted, and whichever
    committed second violated the unique index — surfacing as a *failed start*, not a failed job.
    It now converges instead: losing the race is success, because the winner produced exactly the
    row the loser wanted. A create that fails for any other reason still throws.
  - Covered by 11 tests. The two that matter were confirmed to fail against the old
    implementation before being kept — including one that **forces** the collision with a barrier,
    after an earlier version of it passed against the buggy seeder because the fake store never
    interleaved.

---

## 2. Decisions adopted

| Decision | Status here |
|---|---|
| **Azure** as the primary deployment target | Not wired yet |
| **Azure Key Vault** for production secrets | **Wired (2026-08-30).** Set `KeyVault__Uri`; off without it. Registered above `SecretsValidator` so vault-supplied values count as configured |
| **Redis** belongs here | **Already wired** — `AddStackExchangeRedisCache` + `DistributedCacheService` (`Infrastructure/DependencyInjection.cs`), instance prefix `RealEstateCRM:`. It needs a `ConnectionStrings:Redis` value to point at a real server |
| **App Insights (backend) + Sentry (frontend)** | **Both wired (2026-08-30).** App Insights on `APPLICATIONINSIGHTS_CONNECTION_STRING`, Sentry on `VITE_SENTRY_DSN`. Each registers only when its value is present. Sentry is dynamically imported — the main chunk moved 448.87 → 448.89 kB, so it costs nothing unconfigured |
| **Move DB seeding and Hangfire init out of startup** | **Done (2026-08-29).** Now `--init`, an explicit deployment step. Development still runs it on startup by design. See `docs/deployment.md` |
| **Navy Corporate theme** | Done |
| **Tailwind alongside existing CSS, not replacing it** | Done. Deliberate: incremental adoption over a risky rewrite |
| **Design system is vendored, not linked** | The copy in `client/real-estate-crm-react/design-system/` is a vendored snapshot. Source of truth is `MeCodex/design-system`; do not hand-edit the copy |

---

## 3. Still open



Key Vault, Application Insights and Sentry are all **wired but inert** — each activates only when
its configuration value is present, so nothing happens until you supply one. What is left is
supplying them, not writing code:

| Needs | Value to set |
|---|---|
| Azure Key Vault | `KeyVault__Uri` |
| Application Insights | `APPLICATIONINSIGHTS_CONNECTION_STRING` |
| Sentry | `VITE_SENTRY_DSN` |
| Redis | `ConnectionStrings:Redis` |

- **Azure deployment** — `azure-deploy.yml` exists but has never run against a real subscription.
- **Redis is wired but unconfigured**, and note the difference from the other three:
  `AddStackExchangeRedisCache` is registered **unconditionally**, so `ConnectionStrings:Redis` must
  point at a running server. There is no in-memory fallback here, unlike PosFlow and Gym Manager
  where the same decision was implemented with one.
- **Hangfire dashboard credentials.** `Hangfire:DashboardUsername` / `DashboardPassword` must be set
  before deploying anywhere network-reachable. The authorization filter falls back if they are
  unset, which is fine locally and not fine in production.

---

## 4. Known issues / technical debt

- **Two styling systems are live at once** — Tailwind and hand-written CSS. That is the deliberate
  cost of incremental adoption, but it does mean two places to look when a style is wrong. The
  Tailwind preset maps `--mx-*` tokens onto Tailwind's scale so the two at least resolve to the
  same values.
- ~~**The vendored design system can drift.**~~ ✅ **Closed 2026-08-30.** CI now clones MeCodex
  (it is public) and diffs the three vendored files. A hand-edit here, or an upstream
  regeneration that was never re-vendored, fails the build in either direction.
  - The comparison uses `diff --strip-trailing-cr`, and that is load-bearing rather than
    defensive: MeCodex commits these files with CRLF while the copy here is committed with LF.
    They are otherwise byte-identical — 5215 vs 5078 bytes on `tokens.css`, exactly the 137 CR
    characters. Without the flag the check would have failed on **every run** and been muted,
    which is worse than not having it. Caught by running the check before shipping it.
- **Frontend bundle is large** — the main chunk is ~449 kB (135 kB gzipped) and the dashboard
  another ~363 kB. It builds fine; it is simply not small.

---

## 5. Deliberately deferred

| Item | Why |
|---|---|
| **Replacing the hand-written CSS with Tailwind** | The screens work and are styled. A wholesale rewrite is regression risk with no user-visible gain; Tailwind is used for new work instead |
| **Linking rather than vendoring the design system** | Would need shared-package infrastructure across separate repos. Vendoring is the honest simple option; the drift risk is recorded above instead of hidden |
