using RealEstateCRM.Domain.Common;

namespace RealEstateCRM.Tests.MultiTenancy;

/// <summary>
/// Stand-in for a future tenant-owned entity (Lead, Unit, Deal, ...). Exists only to prove
/// the generic ITenantEntity/TenantEntity isolation mechanism works before those entities exist.
/// </summary>
internal class TestTenantEntity : TenantEntity
{
    public string Name { get; set; } = string.Empty;
}
