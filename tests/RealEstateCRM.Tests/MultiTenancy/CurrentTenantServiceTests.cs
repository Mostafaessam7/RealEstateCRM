using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Infrastructure.Auth;
using Xunit;

namespace RealEstateCRM.Tests.MultiTenancy;

public class CurrentTenantServiceTests
{
    private static CurrentTenantService CreateService(ClaimsPrincipal? user)
    {
        var httpContext = new DefaultHttpContext();
        if (user is not null)
        {
            httpContext.User = user;
        }

        var accessor = new HttpContextAccessor { HttpContext = user is null ? null : httpContext };
        return new CurrentTenantService(accessor);
    }

    [Fact]
    public void ResolvesUserIdAndCompanyId_FromClaims()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("company_id", companyId.ToString()),
            new Claim(ClaimTypes.Role, Roles.CompanyAdmin)
        }, "TestAuth");

        var service = CreateService(new ClaimsPrincipal(identity));

        Assert.Equal(userId, service.UserId);
        Assert.Equal(companyId, service.CompanyId);
        Assert.False(service.IsSuperAdmin);
    }

    [Fact]
    public void IsSuperAdmin_True_ForSuperAdminRole_WithNoCompanyId()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, Roles.SuperAdmin)
        }, "TestAuth");

        var service = CreateService(new ClaimsPrincipal(identity));

        Assert.True(service.IsSuperAdmin);
        Assert.Null(service.CompanyId);
    }

    [Fact]
    public void NoHttpContext_ResolvesToNulls_NotSuperAdmin()
    {
        var service = CreateService(user: null);

        Assert.Null(service.UserId);
        Assert.Null(service.CompanyId);
        Assert.False(service.IsSuperAdmin);
    }
}
