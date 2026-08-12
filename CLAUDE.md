# CLAUDE.md

## Project

Real Estate CRM SaaS.

A multi-tenant CRM platform for real estate companies to manage leads, projects, units, deals, follow-ups, commissions, users, and reports.

## Tech Stack

### Backend
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- JWT Access Tokens
- Refresh Tokens
- Redis
- Hangfire
- SignalR
- Azure Blob Storage

### Frontend
- React
- TypeScript
- React Router
- TanStack Query
- React Hook Form
- Zod

### Infrastructure
- Azure
- Docker

## Architecture

Use Clean Architecture.

```text
RealEstateCRM.sln

src/
├── RealEstateCRM.Domain
├── RealEstateCRM.Application
├── RealEstateCRM.Infrastructure
└── RealEstateCRM.Api

client/
└── real-estate-crm-react
```

Use a modular monolith.

Do NOT introduce microservices unless explicitly requested.

---

# Source of Truth

Project documentation is stored under `docs/`.

Use these files as persistent project memory:

- Architecture → `docs/architecture.md`
- Database → `docs/database.md`
- Authentication → `docs/auth.md`
- Multi-tenancy → `docs/multi-tenancy.md`
- API conventions → `docs/api.md`
- Frontend → `docs/frontend.md`
- Decisions → `docs/decisions.md`
- Current status → `docs/roadmap.md`

Only read documentation relevant to the current task.

Do NOT repeatedly summarize these documents.

---

# Token Efficiency

Token efficiency is important.

Follow these rules strictly.

1. Do not inspect the entire repository unless explicitly requested.
2. Read only files directly relevant to the current task.
3. Do not repeatedly read unchanged files.
4. Prefer targeted file search.
5. Prefer targeted text search.
6. Do not repeat requirements already documented.
7. Do not output unchanged code.
8. Prefer small patches over rewriting complete files.
9. Do not generate large boilerplate unless necessary.
10. Do not explain obvious code unless asked.
11. Work on one task at a time.
12. Do not implement future roadmap items.
13. Do not refactor unrelated code.
14. Do not automatically summarize the repository.
15. Avoid speculative abstractions.
16. Do not create files that are not currently needed.
17. Do not run broad commands when targeted commands are sufficient.
18. Keep completion messages short.

---

# Task Workflow

For every task:

1. Read `CLAUDE.md`.
2. Read `docs/roadmap.md` only when roadmap context is needed.
3. Read only relevant documentation.
4. Inspect the minimum code required.
5. Identify the smallest correct implementation.
6. Make the change.
7. Run the smallest useful validation.
8. Fix only errors caused by the change.
9. Update roadmap only if task status changed.
10. Stop.

Do NOT automatically continue to another roadmap item.

---

# Backend Rules

Use:
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server

Code requirements:

- Use async/await.
- Use CancellationToken where appropriate.
- Enable nullable reference types.
- Use dependency injection.
- Keep controllers thin.
- Business logic belongs in Application.
- Infrastructure logic belongs in Infrastructure.
- Domain must not depend on Infrastructure.
- Use request/response DTOs.
- Never expose EF Core entities directly from API endpoints.
- Use FluentValidation where useful.
- Prefer explicit readable code.
- Avoid unnecessary abstraction.

Do NOT create a generic repository over EF Core.

Do NOT introduce CQRS/MediatR everywhere.

Use them only when there is a concrete benefit.

---

# Multi-Tenancy

Each real estate company is a tenant.

Tenant-owned entities must contain:

```text
CompanyId
```

Never trust CompanyId supplied by the frontend.

Resolve CompanyId from authenticated context.

Use EF Core global query filters where appropriate.

All tenant-owned reads/writes must enforce tenant isolation.

See:

`docs/multi-tenancy.md`

---

# Authentication

Use:

- ASP.NET Core Identity
- JWT access tokens
- Refresh tokens
- Roles
- Policies

Roles:

- SuperAdmin
- CompanyAdmin
- SalesManager
- SalesAgent

See:

`docs/auth.md`

---

# Security

Never:

- hardcode secrets
- commit production credentials
- log passwords
- log JWT access tokens
- store refresh tokens as plaintext
- trust roles sent from frontend
- trust CompanyId sent from frontend
- disable authorization to fix a bug
- expose cross-tenant data
- expose stack traces in production

---

# API

Use REST.

Use:

- DTOs
- validation
- pagination
- filtering
- sorting
- search
- proper HTTP status codes
- consistent errors

See:

`docs/api.md`

---

# Validation

Prefer targeted validation.

Backend:

```bash
dotnet build
```

Tests:

```bash
dotnet test
```

Frontend:

```bash
npm run build
```

Run a specific project/test when possible instead of the entire solution.

---

# Documentation Updates

Do not update documentation for trivial implementation details.

Update docs only when:

- architecture changes
- database design changes
- authentication strategy changes
- multi-tenancy behavior changes
- API conventions change
- roadmap task status changes

---

# Completion Response

At the end of a task respond only with:

- What changed
- Files changed
- Validation performed
- Blocker, if any

Keep the response concise.