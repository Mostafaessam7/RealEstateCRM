\# Architecture



\## Goal



Build a maintainable multi-tenant Real Estate CRM SaaS.



The application allows real estate companies to manage:



\- leads

\- sales agents

\- projects

\- units

\- deals

\- follow-ups

\- commissions

\- tasks

\- reports



\## Architecture Style



Use a modular monolith with Clean Architecture.



Do not use microservices initially.



\## Solution



```text

RealEstateCRM.sln



src/

├── RealEstateCRM.Domain

├── RealEstateCRM.Application

├── RealEstateCRM.Infrastructure

└── RealEstateCRM.Api



client/

└── real-estate-crm-react

mobile/

└── Flutter (Dart) client — field-agent app, consumes the Public API (`/api/v1`, see

&#x20;   `docs/public-api.md`), same JWT auth/roles as the web app. Feature-based structure

&#x20;   (`domain → data → application → presentation` per feature), Riverpod state management,

&#x20;   Dio networking. Replaced an earlier Expo/React Native client — see `docs/roadmap.md`.

```



\## Domain



Contains:



\- Entities

\- Enums

\- Value Objects when justified

\- Domain rules

\- Domain-specific interfaces when necessary



Domain must not depend on:



\- EF Core

\- ASP.NET Core

\- Redis

\- Azure

\- Hangfire

\- Infrastructure



\## Application



Contains:



\- Use cases

\- Application services

\- DTOs

\- Validators

\- Infrastructure abstractions

\- Authorization/business rules where appropriate



Depends on:



\- Domain



\## Infrastructure



Contains:



\- EF Core

\- SQL Server

\- ASP.NET Core Identity implementation

\- Redis

\- Hangfire

\- Azure Blob Storage

\- Email implementations

\- External integrations



Depends on:



\- Domain

\- Application



\## API



Contains:



\- Controllers/endpoints

\- Authentication configuration

\- Authorization configuration

\- Middleware

\- Dependency injection composition

\- API configuration



Depends on:



\- Application

\- Infrastructure



\## Frontend



React + TypeScript SPA.



Frontend communicates with ASP.NET Core through HTTPS REST APIs.



SignalR is used where realtime communication provides value.



\## Architecture Principles



\- Keep controllers thin.

\- Keep business rules out of controllers.

\- Keep infrastructure concerns outside Domain.

\- Prefer simple solutions.

\- Avoid premature abstraction.

\- Avoid generic repositories over EF Core.

\- Do not introduce microservices.

\- Do not introduce message brokers unless required later.

\- Tenant isolation is mandatory.

\- Security is enforced by backend, not frontend.



\## Initial Modules



\- Companies

\- Users

\- Leads

\- Lead Activities

\- Projects

\- Units

\- Deals

\- Tasks

\- Commissions

\- Notifications

\- Reports



\## Later



All of the items originally listed here (AI Lead Assistant, WhatsApp automation, subscriptions,

online payments, mobile apps, public marketplace, advanced recommendation engine, external

developer APIs) have since been explicitly requested and implemented — see `docs/roadmap.md`

Phases 19–21 for what each one is and how it's built. Nothing remains deferred. Add new items

here only if the user again defers something they don't want built yet.

