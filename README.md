# Real Estate CRM

A multi-tenant CRM SaaS for real estate companies — leads, projects, units, deals, follow-ups,
commissions, users, and reports. Clean Architecture, modular monolith. See
[`CLAUDE.md`](CLAUDE.md) for the full tech stack and working rules.

## Structure

```text
src/                          ASP.NET Core backend (Domain / Application / Infrastructure / Api)
tests/                        Backend test suite (xUnit)
client/real-estate-crm-react/ Web frontend (React + TypeScript + Vite)
mobile/                       Flutter mobile client
docs/                         Architecture, database, auth, API, deployment, roadmap
```

## Documentation

Start here — these are the persistent source of truth, not this file:

- [Architecture](docs/architecture.md)
- [Database](docs/database.md)
- [Authentication](docs/auth.md)
- [Multi-tenancy](docs/multi-tenancy.md)
- [Internal API conventions](docs/api.md)
- [Public API (`/api/v1`) & webhooks](docs/public-api.md)
- [Frontend conventions](docs/frontend.md)
- [Deployment](docs/deployment.md)
- [Decisions log](docs/decisions.md)
- [Roadmap / phase history](docs/roadmap.md)

## Run it locally

**Fastest path — Docker Compose** (backend + web + SQL Server + Redis + Azurite):

```bash
cp .env.example .env   # fill in SQL_SA_PASSWORD and JWT_KEY
docker compose up --build
```

See [`docs/deployment.md`](docs/deployment.md) for the full local/Azure setup, including the
one-time `dotnet ef database update` migration step Compose does not run automatically.

**Backend only:**

```bash
dotnet build
dotnet test
dotnet run --project src/RealEstateCRM.Api
```

**Web frontend:** see [`client/real-estate-crm-react/README.md`](client/real-estate-crm-react/README.md).

**Mobile (Flutter):** see [`mobile/README.md`](mobile/README.md).

## Validation

```bash
dotnet build && dotnet test                                   # backend
npm run lint && npm test && npm run build --prefix client/real-estate-crm-react   # web
cd mobile && flutter analyze && flutter test                  # mobile
```
