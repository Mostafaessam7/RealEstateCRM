# Frontend

## Stack

Use:

- React
- TypeScript
- React Router
- TanStack Query
- React Hook Form
- Zod
- Axios or equivalent HTTP client

### Styling

- **Hand-written CSS against the design-system tokens.** There is no CSS framework. Components
  style themselves with ordinary classes and custom properties.

  Tailwind was previously listed here as being live "alongside" this CSS. It never was. It was
  installed and configured, but the client has no `postcss.config.*` and no Tailwind plugin in
  `vite.config.ts`, so nothing ever processed it: the `@tailwind` directives were copied verbatim
  into the shipped stylesheet as invalid at-rules and not one utility class was generated. No
  component used a Tailwind class either. It was removed in full on 2026-09-04; the built CSS
  shrank by exactly the 56 bytes of those three directives and was otherwise byte-identical, which
  is the proof it had no effect.

- **The shared design system**, vendored into `client/real-estate-crm-react/design-system/`:

  ```css
  /* src/index.css — order matters */
  @import '../design-system/tokens.css';                  /* theme-independent; carries NO colour */
  @import '../design-system/themes/navy-corporate.css';   /* this product's colour */
  ```

  Note the `../`: `design-system/` sits at the client root, not under `src/`.

- **Theme: Navy Corporate.** Every product in the workspace has its own theme over one token
  architecture, and all themes expose an **identical set of token names** — so a component written
  against `--mx-surface` is portable across products.

- The design system is **vendored, not linked.** Its source of truth is `MeCodex/design-system`;
  the theme files there are generated and contrast-verified. Do not hand-edit the copy here — a
  local edit will be silently overwritten the next time it is re-vendored, and it will no longer
  match the generator's contrast guarantees.

## Structure

Suggested:

```text
client/
└── real-estate-crm-react/
    └── src/
        ├── api/
        ├── app/
        ├── components/
        ├── features/
        ├── hooks/
        ├── layouts/
        ├── pages/
        ├── routes/
        ├── types/
        └── utils/
```

## Feature Organization

Prefer feature-based organization.

```text
features/
├── auth/
├── dashboard/
├── leads/
├── projects/
├── units/
├── deals/
├── tasks/
├── commissions/
└── users/
```

## Server State

Use TanStack Query. Use it for:

- fetching
- caching
- mutations
- invalidation
- loading states
- server synchronization

Do not add another global state library without a real need.

## Forms

Use:

- React Hook Form
- Zod

Frontend validation improves UX. Backend validation remains authoritative.

## Authentication

Frontend should support:

- login
- logout
- protected routes
- role-aware navigation
- session refresh
- expired-session handling

Do not treat hidden buttons as authorization. Backend must always enforce permissions.

## API Client

Centralize:

- Base URL
- Authentication handling
- Refresh behavior
- Error handling

Do not create separate Axios configuration in every feature.

## Initial Pages

```text
Login
Dashboard
Leads
Lead Details
Pipeline
Projects
Units
Unit Details
Deals
Tasks
Commissions
Reports
Users
Company Settings
```

## Later-Phase Pages

Added in Phases 19–21, beyond the initial MVP page list above:

```text
Billing (/billing)                     — current plan, usage bars, plan comparison
WhatsApp Templates (/whatsapp-templates)
Marketing Campaigns (/marketing-campaigns)
API Keys (/api-keys)
Webhooks (/webhooks)
Marketplace (/marketplace)             — public, unauthenticated, outside MainLayout
```

See `docs/roadmap.md` Phases 19–21 for what each does.

## Layout

Primary CRM layout:

```text
Sidebar
Top Navbar
Page Header
Main Content
```

## Dashboard

Initial KPIs:

- Total Leads
- New Leads
- Conversion Rate
- Total Deals
- Total Sales Value
- Upcoming Follow-ups
- Available Units

Additional (on the Reports page, not the Dashboard):

- Leads by status
- Leads by source
- Agent performance

The Dashboard itself later gained two more panels beyond the KPI grid: a "Leads Pipeline" bar chart (leads by status, using the same status→color mapping as `StatusBadge` everywhere else) and a "Recent Activity" feed merging the newest leads and deals into one chronological list. See `client/.../src/features/dashboard/LeadsPipelineChart.tsx` and `RecentActivity.tsx`.

## Leads

Support:

- table
- search
- filters
- pagination
- sorting
- create lead
- edit lead
- lead details
- activity timeline
- follow-ups
- assignment

## Pipeline

Kanban columns:

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

Do not implement complex drag/drop behavior until backend business rules are established.

## Async UX

Every asynchronous page should handle:

- Loading
- Empty
- Error
- Success

## Responsive Design

Primary target: Desktop. Also support:

- Tablet
- Mobile

Do not build a native mobile application initially.

## Performance

Avoid:

- unnecessary global state
- unnecessary rerenders
- fetching the same server data manually in multiple places

Use TanStack Query caching appropriately.
