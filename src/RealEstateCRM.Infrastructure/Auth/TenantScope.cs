using RealEstateCRM.Application.Common.Interfaces;

namespace RealEstateCRM.Infrastructure.Auth;

public class TenantScope : ITenantScope
{
    public IDisposable Begin(Guid companyId) => AmbientTenantContext.Begin(companyId);
}
