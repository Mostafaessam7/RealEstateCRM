# Multi-Tenancy

## Strategy

Use:

- Shared database
- Shared schema
- CompanyId tenant discriminator

Each real estate company is one tenant.

## Core Rule

Authenticated context is the authority for tenant identity. Never trust CompanyId supplied by normal frontend requests.

## Tenant-Owned Entities

Tenant-owned entities must contain:

```text
CompanyId
```

Examples:

- Lead
- LeadActivity
- Project
- Unit
- Deal
- Commission
- TaskItem
- Notification

## Current Tenant Service

Create an abstraction similar to:

```csharp
public interface ICurrentTenantService
{
    Guid? CompanyId { get; }
    Guid? UserId { get; }
    bool IsSuperAdmin { get; }
}
```

Exact implementation can change if there is a better reason.

## Resolution

For normal users:

1. Authenticate JWT.
2. Resolve authenticated UserId.
3. Resolve CompanyId from trusted authenticated context.
4. Use this CompanyId for tenant operations.

## EF Core

Use EF Core Global Query Filters where appropriate. Conceptually:

```text
Entity.CompanyId == CurrentTenant.CompanyId
```

Combine with soft delete filtering where necessary.

## Creating Records

When creating tenant-owned records: Never do:

```text
CompanyId = request.CompanyId
```

Instead:

```text
CompanyId = CurrentTenant.CompanyId
```

Tenant create DTOs should generally not contain CompanyId.

## Reading Records

Always query within tenant scope. Prefer:

```text
WHERE Id = requestedId
AND CompanyId = currentCompanyId
```

Do not load arbitrary IDs across tenants and rely only on frontend checks.

## Updating

A tenant must never update another tenant's entity by guessing its ID.

## Deleting

Tenant isolation also applies to:

- delete
- soft delete
- restore

## Background Jobs

Hangfire jobs do not automatically have HTTP tenant context. Tenant-specific jobs must receive a trusted CompanyId when scheduled. The job must restore/use the tenant scope explicitly. Never depend on HttpContext inside background jobs.

## Redis

Tenant-specific cache keys must be namespaced. Examples:

```text
tenant:{companyId}:dashboard
tenant:{companyId}:settings
tenant:{companyId}:units:available
```

Never share tenant-specific cache entries.

## SignalR

Connections may receive:

- user-specific notifications
- authorized tenant group notifications

Never globally broadcast tenant business data.

## Blob Storage

Prefer tenant-scoped paths:

```text
companies/{companyId}/projects/{projectId}/...
companies/{companyId}/units/{unitId}/...
```

Blob paths are not an authorization mechanism. Application authorization remains mandatory.

## SuperAdmin

SuperAdmin cross-tenant access must be deliberate. Do not globally disable tenant query filters for every SuperAdmin request. Create explicit platform administration operations when needed.

## Tests

Tenant isolation integration tests are mandatory. Test at least:

- Company A cannot read Company B lead.
- Company A cannot update Company B lead.
- Company A cannot delete Company B lead.
- Company A cannot read Company B unit.
- Company A cannot access Company B deal.
- guessed IDs cannot bypass isolation.
- cache keys cannot leak tenant data.
