using RealEstateCRM.Application.Common.Interfaces;

namespace RealEstateCRM.Tests.MultiTenancy;

internal class FakeCurrentTenantService : ICurrentTenantService
{
    public Guid? CompanyId { get; set; }
    public Guid? UserId { get; set; }
    public bool IsSuperAdmin { get; set; }
    public HashSet<string> Roles { get; set; } = new();

    public bool IsInRole(string role) => Roles.Contains(role);
}
