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
- **Shared design system adopted.** Styling is hand-written CSS against the design-system tokens;
  there is no CSS framework.
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
| **No CSS framework; hand-written CSS against the tokens** | Settled 2026-09-04. Tailwind had been recorded here as adopted, but it was never wired up and was removed. See section 6 |
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

- ~~**Two styling systems are live at once.**~~ ✅ **Closed 2026-09-04.** This was recorded as the
  deliberate cost of incremental Tailwind adoption. It was never true: Tailwind was installed and
  configured but never wired into the build, so it produced nothing and no component used it.
  There has only ever been one styling system here. Tailwind has been removed outright.
- ~~**The vendored design system can drift.**~~ ⚠️ **Closed 2026-08-30, degraded 2026-09-03.** CI
  clones MeCodex and diffs the vendored files (two of them since 2026-09-04 — the Tailwind preset
  is no longer vendored). MeCodex is **private** again, so the clone fails and the step now warns
  loudly and skips rather than asserting a drift it never measured. It is not currently a gate.
  Making MeCodex public, or giving this job a read token, restores it. A hand-edit here, or an upstream
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
| **Adopting a CSS framework** | The screens work and are styled against the shared tokens. A wholesale rewrite is regression risk with no user-visible gain. An earlier attempt to add Tailwind incrementally was never wired up and went unnoticed for weeks, which is itself an argument for not carrying one |
| **Linking rather than vendoring the design system** | Would need shared-package infrastructure across separate repos. Vendoring is the honest simple option; the drift risk is recorded above instead of hidden |

---

## 6. Update 2026-09-04 — Tailwind removed; CI audit hardened

**Tailwind was never wired up, and has been removed.** It was installed, configured with a preset,
and referenced from `src/index.css`, and this document recorded it as adopted. None of that was
working. The client has no `postcss.config.*` anywhere and no Tailwind plugin in `vite.config.ts`,
so Vite passed the three `@tailwind` directives straight through into the shipped stylesheet as
invalid at-rules and generated no utilities at all. Nothing depended on it either: across the 45
`.tsx` files that use `className` there is not one Tailwind utility.

Verified rather than assumed before deleting anything: the built CSS was 17726 bytes before and
17670 after, a difference of exactly 56 bytes, which is exactly the length of
`@tailwind base;@tailwind components;@tailwind utilities;`. Stripping that one string from the old
output makes it byte-identical to the new output, so the rendered result cannot have changed.
37 tests pass.

Removed: `tailwindcss`, `tailwindcss-animate`, and `postcss`/`autoprefixer` (which existed only to
serve Tailwind and, with no PostCSS config, also did nothing); `tailwind.config.js`; and the
vendored `design-system/tailwind-preset.js`, which this app has no remaining use for. The CI drift
check no longer compares that preset, because the file is no longer vendored here.

**Corrected the design-system import paths.** `design-system/` sits at the client root, not under
`src/`, so `./design-system/tokens.css` from `src/index.css` pointed at a directory that does not
exist. It resolved only because Vite falls back to the project root when a relative CSS `@import`
misses — working by accident, and the kind of thing that breaks silently on a bundler upgrade. Now
`../`, which is where the files actually are.

**CI no longer reads an npm outage as a security finding.** `npm audit` exits non-zero both when it
finds an advisory and when it cannot reach the registry, and the step treated those identically. On
2026-09-04 a burst of 503s from npm's audit endpoint failed five unrelated dependency PRs across
these repositories. The step now retries the transport, still fails hard on a genuine High/Critical
finding, and if the registry stays unreachable says loudly that the audit did **not** run rather
than reporting a clean pass.

**Still open here:** the `microsoft` dependency group bump (PR #22) does not build. The
`Microsoft.AspNetCore.OpenApi` source generator emits `IOpenApiMediaType.Example = ...` while the
`Microsoft.OpenApi` version in that group made the property read-only (CS0200, twice, in generated
code). That is an upstream mismatch, not something to patch here, so the PR stays open until a
compatible combination ships.

---

## Update 2026-09-04 — one branch, protected; routine dependency PRs off

**This repo keeps a single branch: `main`.** Any leftover Dependabot branches were deleted and no
long-lived branches are kept.

**`main` is protected**, and the protection is deliberately the kind that fits a one-branch
workflow:

| Setting | Value | Why |
|---|---|---|
| Force pushes | **blocked** | History cannot be rewritten or silently rolled back. Verified by attempting one and having it rejected |
| Branch deletion | **blocked** | `main` cannot be removed |
| Applies to admins | **yes** | The owner is not exempt; that exemption was the hole fixed on E-Commerce earlier |
| Required status checks | **none** | Deliberate trade-off. Required checks make direct pushes to `main` impossible and force every change through a branch and PR, which is exactly what the one-branch decision rules out. CI still runs on every push — it reports rather than gates |

**Routine dependency PRs are off.** Every `open-pull-requests-limit` in `.github/dependabot.yml` is
`0`, because weekly version bumps meant a continuous stream of branches to merge or close.
**Security updates are unaffected** — Dependabot ignores that limit for security advisories, so a
dependency with a known vulnerability still opens a PR. Set the limits back to a non-zero number to
bring routine updates back.
