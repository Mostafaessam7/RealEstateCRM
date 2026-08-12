namespace RealEstateCRM.Application.Common.Interfaces;

/// <summary>
/// Lets code with no HTTP context (background jobs) explicitly declare which tenant it is
/// currently acting for, so ICurrentTenantService/tenant-safe writes still work correctly.
/// See docs/multi-tenancy.md#background-jobs — never depend on HttpContext inside a job;
/// jobs must instead process one tenant at a time inside a scope like this.
/// </summary>
public interface ITenantScope
{
    IDisposable Begin(Guid companyId);
}
