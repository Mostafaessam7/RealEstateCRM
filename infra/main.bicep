// Azure resources this application needs, as code.
//
// ---------------------------------------------------------------------------------------------
// READ THIS BEFORE TRUSTING IT
//
// This template has NEVER BEEN DEPLOYED. No Azure subscription has been available, so what is
// verified is exactly one thing: `az bicep build` compiles it, which CI runs on every push. That
// catches syntax errors, unknown resource types, bad API versions and wrong property names. It
// does NOT prove a deployment succeeds, that the SKUs are available in your region, that the
// quota exists, or that the app actually starts against these resources.
//
// The first `az deployment group create` will surface problems. That is expected. This exists so
// the shape of the environment is written down and reviewable instead of living in whoever set up
// the portal by hand -- which today is nobody, because nothing has been set up at all.
// ---------------------------------------------------------------------------------------------
//
// Why these resources: they are not a guess, they are what .github/workflows/azure-deploy.yml
// already assumes exists. That workflow pushes two images to a registry, runs EF migrations
// against a SQL connection string, and deploys both images to named App Services. The app itself
// additionally reads Redis, Key Vault and Application Insights configuration at startup.

targetScope = 'resourceGroup'

@description('Short prefix for every resource name. Lowercase letters and digits only; some resource types reject hyphens and uppercase.')
@minLength(3)
@maxLength(11)
param namePrefix string

@description('Region for every resource. Defaults to the resource group location so the two cannot drift apart.')
param location string = resourceGroup().location

@description('Deployment environment. Only affects naming and the App Service always-on setting.')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

@description('Administrator login for the SQL server. Cannot be changed after creation.')
param sqlAdministratorLogin string

@description('Administrator password for the SQL server. Pass from Key Vault or a pipeline secret, never a parameter file in source control.')
@secure()
param sqlAdministratorPassword string

// Free/Basic tiers are the default so a first deployment cannot quietly cost real money while
// nobody is watching it. Raise these deliberately.
@description('App Service plan SKU.')
param appServicePlanSku string = 'B1'

@description('Azure SQL database SKU.')
param sqlDatabaseSku string = 'Basic'

var suffix = uniqueString(resourceGroup().id)
var baseName = '${namePrefix}-${environment}'
// Registry names allow neither hyphens nor uppercase, unlike everything else here.
var registryName = toLower('${namePrefix}${environment}acr${substring(suffix, 0, 6)}')
var keyVaultName = take('${baseName}-kv-${substring(suffix, 0, 6)}', 24)
var alwaysOn = environment != 'dev' // B1 supports always-on, but it costs; off in dev.

// ---------------------------------------------------------------------------------------------
// Observability. Created first so everything else can point at it.
// ---------------------------------------------------------------------------------------------

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${baseName}-logs'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

// Workspace-based, because classic Application Insights is retired.
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${baseName}-ai'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// ---------------------------------------------------------------------------------------------
// Data
// ---------------------------------------------------------------------------------------------

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${baseName}-sql'
  location: location
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'realestatecrm'
  location: location
  sku: {
    name: sqlDatabaseSku
  }
}

// Lets App Service and the GitHub runner reach SQL. This is the blunt "all Azure services" rule;
// tighten it to a private endpoint before this holds anything real.
resource sqlAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource redis 'Microsoft.Cache/redis@2024-03-01' = {
  name: '${baseName}-redis'
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 0
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
}

// ---------------------------------------------------------------------------------------------
// Secrets. RBAC rather than access policies -- access policies are the older model and cannot be
// assigned to a managed identity that does not exist yet without a second deployment pass.
// ---------------------------------------------------------------------------------------------

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

// ---------------------------------------------------------------------------------------------
// Container registry and the two apps
// ---------------------------------------------------------------------------------------------

resource registry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: registryName
  location: location
  sku: { name: 'Basic' }
  properties: {
    // The deploy workflow authenticates with ACR_USERNAME / ACR_PASSWORD, which are the admin
    // credentials. Turn this off and move to a service principal or managed identity when the
    // workflow can be changed with it.
    adminUserEnabled: true
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${baseName}-plan'
  location: location
  sku: { name: appServicePlanSku }
  kind: 'linux'
  properties: {
    reserved: true // "reserved" means Linux. It does not mean reserved capacity.
  }
}

// Both apps share one plan and differ only in image and configuration, so they are one module
// expressed twice rather than two near-identical blocks.
var apps = [
  {
    name: '${baseName}-api'
    image: 'realestatecrm-api'
    isApi: true
  }
  {
    name: '${baseName}-client'
    image: 'realestatecrm-client'
    isApi: false
  }
]

resource webApps 'Microsoft.Web/sites@2023-12-01' = [for app in apps: {
  name: app.name
  location: location
  identity: {
    // System-assigned, so the app can read Key Vault without a secret of its own. This is the
    // identity granted the Key Vault role below.
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOCKER|${registry.properties.loginServer}/${app.image}:latest'
      alwaysOn: alwaysOn
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      healthCheckPath: app.isApi ? '/health' : '/'
      appSettings: concat(
        [
          {
            name: 'DOCKER_REGISTRY_SERVER_URL'
            value: 'https://${registry.properties.loginServer}'
          }
          {
            name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
            value: appInsights.properties.ConnectionString
          }
          {
            name: 'WEBSITES_PORT'
            value: '8080'
          }
        ],
        // Only the API talks to SQL, Redis and Key Vault. The client is a static bundle behind
        // nginx and has no business holding a connection string.
        app.isApi
          ? [
              {
                name: 'KeyVault__Uri'
                value: keyVault.properties.vaultUri
              }
              {
                name: 'ConnectionStrings__Redis'
                value: '${redis.properties.hostName}:${redis.properties.sslPort},ssl=True,abortConnect=False'
              }
            ]
          : []
      )
    }
  }
}]

// Key Vault Secrets User. Lets the API read secrets but not write or manage them.
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource apiKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  // Deterministic name: re-running the deployment must reuse the assignment, not fail on a
  // duplicate or create a second one.
  name: guid(keyVault.id, webApps[0].id, keyVaultSecretsUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: webApps[0].identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ---------------------------------------------------------------------------------------------
// Outputs. These are the values the deploy workflow expects as repository variables and secrets;
// naming them here means the wiring does not have to be rediscovered from the portal.
// ---------------------------------------------------------------------------------------------

@description('Set as the AZURE_API_APP_NAME repository variable.')
output apiAppName string = apps[0].name

@description('Set as the AZURE_CLIENT_APP_NAME repository variable.')
output clientAppName string = apps[1].name

@description('Set as the ACR_LOGIN_SERVER secret.')
output registryLoginServer string = registry.properties.loginServer

@description('Set as the KeyVault__Uri setting. Already applied to the API app above.')
output keyVaultUri string = keyVault.properties.vaultUri

@description('Fully qualified SQL server name. Build AZURE_SQL_CONNECTION_STRING from this; the password is not an output on purpose.')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('Application Insights connection string, already applied to both apps.')
output appInsightsConnectionString string = appInsights.properties.ConnectionString
