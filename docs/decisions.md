# Decisions

This file records decisions already made. Claude should not repeatedly reconsider these decisions unless explicitly requested.

## Backend

- ASP.NET Core Web API
- Entity Framework Core
- SQL Server

## Frontend

- React
- TypeScript

Do not use Blazor.

## Authentication

Use:

- ASP.NET Core Identity
- JWT Access Tokens
- Refresh Tokens
- Roles
- Policies

## Infrastructure

Use:

- Redis
- Hangfire
- SignalR
- Azure Blob Storage
- Azure
- Docker

## Architecture

Use:

- Clean Architecture
- Modular Monolith

Do NOT use initially:

- Microservices
- Message broker
- Event-driven distributed architecture
- Generic Repository pattern
- unnecessary CQRS
- unnecessary MediatR

## Multi-Tenancy

Use:

```text
Shared Database
Shared Schema
CompanyId
```

Use EF Core global query filters where appropriate. CompanyId comes from authenticated context. Never trust CompanyId from normal frontend requests.

## Product

Product: Real Estate CRM SaaS. Primary users:

```text
SuperAdmin
CompanyAdmin
SalesManager
SalesAgent
```

## Initial Scope

Build:

- Companies
- Users
- Leads
- Lead Activities
- Projects
- Units
- Deals
- Follow-ups
- Tasks
- Commissions
- Notifications
- Dashboard
- Reports

## Not Initial Scope

This section originally listed items to defer unless explicitly requested. Every item below has since been explicitly requested and implemented — see `docs/roadmap.md` Phases 19–21 for what each one is and how it's built. Nothing in this list remains undone:

- ~~AI Assistant~~ — implemented, Phase 19.
- ~~WhatsApp automation~~ — implemented, Phase 19.
- ~~mobile application~~ — implemented, Phase 21; rebuilt in Flutter in Phase 23.
- ~~public property marketplace~~ — implemented, Phase 21.
- microservices — still correctly out of scope; see `## Architecture` above.
- ~~complex billing~~ — implemented as Billing & Subscriptions, Phase 19.
- ~~payment gateway~~ — implemented as Stripe-backed Online Payments, Phase 21.
- ~~marketing automation~~ — implemented, Phase 20.
- ~~external public API~~ — implemented as the versioned `/api/v1` Public API, Phase 20.

Add new items here only if the user again defers something they don't want built yet.
