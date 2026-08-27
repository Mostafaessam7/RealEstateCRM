# Deployment

## Local (Docker Compose)

```text
cp .env.example .env   # fill in SQL_SA_PASSWORD and JWT_KEY
docker compose up --build
```

Services: `api` (http://localhost:5063), `client` (http://localhost:5173), `sqlserver`, `redis`, `azurite` (local Blob Storage emulator). Migrations are not applied automatically on container start. Run them from the host once the `sqlserver` container is healthy:

```text
dotnet ef database update \
  --project src/RealEstateCRM.Infrastructure/RealEstateCRM.Infrastructure.csproj \
  --startup-project src/RealEstateCRM.Api/RealEstateCRM.Api.csproj \
  --connection "Server=localhost;Database=RealEstateCRM;User Id=sa;Password=<SQL_SA_PASSWORD>;TrustServerCertificate=True"
```

## Azure

Required resources (names are examples — match whatever you actually provision):

```text
Resource Group          rg-realestatecrm
Azure Container Registry acrrealestatecrm
Azure SQL Database      sql-realestatecrm
Azure Cache for Redis   redis-realestatecrm
Azure Storage Account   strealestatecrm (Blob Storage)
Web App for Containers  app-realestatecrm-api
Web App for Containers  app-realestatecrm-client
```

App Service configuration (Application Settings) for `app-realestatecrm-api`, matching the same environment-variable names used in docker-compose.yml:

```text
ASPNETCORE_ENVIRONMENT           = Production
ConnectionStrings__DefaultConnection  = <Azure SQL connection string>
ConnectionStrings__Redis              = <Azure Cache for Redis connection string>
ConnectionStrings__AzureBlobStorage   = <Storage Account connection string>
Jwt__Key                              = <random 32+ char secret, Key Vault reference preferred>
Jwt__Issuer                           = RealEstateCRM
Jwt__Audience                         = RealEstateCRM
Cors__AllowedOrigins__0               = https://<client app>.azurewebsites.net
BlobStorage__ContainerName             = media
```

Never put real secrets directly in App Service settings for production — use Key Vault references (`@Microsoft.KeyVault(...)`) for `ConnectionStrings__DefaultConnection`, `Jwt__Key`, and `ConnectionStrings__AzureBlobStorage`. CI/CD: `.github/workflows/azure-deploy.yml` first runs `test` (`dotnet build`/`dotnet test`, `npm ci`/`lint`/`build`) and `test-mobile` (`flutter pub get`, `dart format --set-exit-if-changed`, `flutter analyze`, `flutter test`) — a failure in either blocks everything downstream. Only then does it build both Docker images, push them to ACR, apply pending EF Core migrations against the target database, and update both Web Apps, on every push to `main`. It needs these GitHub repo secrets/variables — see the workflow file's header comment for the full list.

## Hangfire dashboard authentication

`/hangfire` is guarded by `HangfireDashboardAuthorizationFilter` (`src/RealEstateCRM.Infrastructure/Auth/HangfireDashboardAuthorizationFilter.cs`): set `Hangfire:DashboardUsername`/`Hangfire:DashboardPassword` (`HANGFIRE_DASHBOARD_USERNAME`/ `HANGFIRE_DASHBOARD_PASSWORD` in `.env`) before deploying anywhere network-reachable — the dashboard then requires HTTP Basic Auth against them. Left unset, it falls back to local-requests-only (the safe default for local dev, same behavior Hangfire's built-in `LocalRequestsOnlyAuthorizationFilter` had before).

## Security headers and hardening

Every response carries `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, and a restrictive `Permissions-Policy` (`Program.cs`) — none of these existed before the QA-pass phase. `app.UseHsts()` runs outside Development. `login`/`refresh`/`logout`/`forgot-password`/`reset-password` are rate-limited (see `docs/auth.md#rate-limiting`). `Microsoft.OpenApi` is pinned to `2.7.5` (direct override of a transitive `2.0.0` reference) to close a known high-severity DoS advisory (GHSA-v5pm-xwqc-g5wc) — re-check this pin on future dependency upgrades.

## Email and WhatsApp delivery

`Smtp:Host` (`SMTP_HOST` in `.env`) — leave blank to run with the logging-only email sender (forgot-password/reset-password emails are logged, not delivered); set it (plus `Smtp:Username`/`Smtp:Password`/etc.) to send real email via SMTP. `WhatsApp:PhoneNumberId`/`WhatsApp:AccessToken` (`WHATSAPP_PHONE_NUMBER_ID`/ `WHATSAPP_ACCESS_TOKEN` in `.env`) — leave blank to run with the logging-only WhatsApp sender; set both to send real messages via Meta's WhatsApp Business Cloud API. Note the Cloud API's own 24-hour customer-service-window rule still applies — this integration sends free-form text messages, not templates, so it only works for leads that messaged the business number recently.

## Known gaps (deliberately out of scope for this phase)

- No HTTPS/TLS termination configured here — Azure Web Apps provide this by default. A

  self-hosted deployment (outside Azure Web Apps for Containers) would need a reverse proxy (nginx/Caddy/Traefik) in front of both containers to terminate TLS — inherently an infrastructure/hosting decision for whoever runs that deployment, not something this repository can configure on their behalf without knowing the target environment.
