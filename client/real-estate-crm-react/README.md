# Real Estate CRM — Web

React + TypeScript SPA for the Real Estate CRM SaaS. See [`docs/architecture.md`](../../docs/architecture.md)
and [`docs/frontend.md`](../../docs/frontend.md) for the full design/structure documentation —
this file only covers running the app locally.

## Stack

React, TypeScript, Vite, React Router, TanStack Query, React Hook Form + Zod, Axios.

## Run it

```bash
npm install
npm run dev      # http://localhost:5173, expects the API at http://localhost:5063
```

Point the API base URL via `VITE_API_BASE_URL` in a `.env` file (see `docker-compose.yml`'s
`client` service for the production build-time equivalent, `VITE_API_BASE_URL` build arg).

## Validation

```bash
npm run lint      # Oxlint
npm test          # Vitest — unit/component tests (src/**/*.test.ts(x))
npm run build     # production build (tsc + vite build)
```

Routes are code-split via `React.lazy` (`src/routes/AppRoutes.tsx`) — each page ships as its own
chunk instead of one monolithic bundle, so the first load only fetches the page actually being
visited.

## Structure

Feature-based under `src/features/` (auth, dashboard, leads, projects, units, deals, tasks,
commissions, users, companies, billing, whatsapp, marketing, developer, marketplace,
notifications, reports), plus shared `api/`, `components/`, `layouts/`, `routes/`, `types/`. See
`docs/frontend.md` for the full page list and layout conventions.
