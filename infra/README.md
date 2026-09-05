# Infrastructure

`main.bicep` describes the Azure resources this application needs.

## What is and is not verified

**This template has never been deployed.** No Azure subscription has been available at any point,
so the only thing verified is that it compiles — CI runs `az bicep build` on every push and fails
on warnings as well as errors.

That is worth something and it is not nothing: compilation resolves every resource type, API
version and property name against the ARM schemas, so a typo or a retired `apiVersion` fails in CI
rather than halfway through someone's first real deployment. But it does **not** prove that a
deployment succeeds, that the SKUs are available in your region, that quota exists, or that the
application starts against these resources.

Expect the first `az deployment group create` to surface problems. Fix them here rather than in the
portal, or this file starts lying the way hand-built environments always do.

## What it creates

Not a guess — this is what `.github/workflows/azure-deploy.yml` already assumes exists, plus what
the application reads at startup.

| Resource | Why |
|---|---|
| Container Registry | The workflow pushes `realestatecrm-api` and `realestatecrm-client` images |
| App Service plan + 2 Linux container apps | The workflow deploys both images by app name |
| Azure SQL server + database | The workflow runs `dotnet ef database update` against it |
| Redis | The API's distributed cache |
| Key Vault | The API reads `KeyVault__Uri` at startup; the API's managed identity is granted *Key Vault Secrets User* |
| Log Analytics + Application Insights | Backend telemetry, per the workspace-wide decision |

Only the API gets the SQL, Redis and Key Vault settings. The client is a static bundle behind nginx
and has no business holding a connection string.

## Deploying it

```bash
az group create --name realestatecrm-dev --location westeurope

az deployment group create \
  --resource-group realestatecrm-dev \
  --template-file infra/main.bicep \
  --parameters namePrefix=recrm environment=dev sqlAdministratorLogin=<login> \
  --parameters sqlAdministratorPassword=<password>
```

Pass the password from a secret store or an interactive prompt. It is a `@secure()` parameter, so
it is not written to deployment history — but a parameters file committed to this repository would
undo that entirely.

## After deploying

The template's outputs are exactly the values the deploy workflow needs. Set them so the workflow
stops being gated off:

| Output | Where it goes |
|---|---|
| `apiAppName` | repository variable `AZURE_API_APP_NAME` |
| `clientAppName` | repository variable `AZURE_CLIENT_APP_NAME` |
| `registryLoginServer` | secret `ACR_LOGIN_SERVER` |
| `sqlServerFqdn` | build `AZURE_SQL_CONNECTION_STRING` from it |

`ACR_USERNAME` / `ACR_PASSWORD` come from the registry's admin credentials, and `AZURE_CREDENTIALS`
from a service principal — neither is an output, because neither should pass through a deployment
log.

## Known shortcuts

Recorded rather than hidden, because each is a decision someone will otherwise have to reverse
engineer:

- **SQL is reachable from all Azure services** (`0.0.0.0` firewall rule). That is how the GitHub
  runner applies migrations. Move to a private endpoint plus a self-hosted runner before this
  database holds anything real.
- **The registry has admin credentials enabled**, because the workflow authenticates with a
  username and password. A service principal or managed identity is better; changing it means
  changing the workflow at the same time.
- **Basic/B1 SKUs throughout**, so a first deployment cannot quietly cost real money. Raise them
  deliberately.
- **No custom domain or certificate.** Both apps come up on `*.azurewebsites.net`.
