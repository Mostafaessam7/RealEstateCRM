using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Users.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Users;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Users;

public class UserManagementServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static (ApplicationDbContext db, UserManager<ApplicationUser> userManager) CreateContext(
        string dbName, FakeCurrentTenantService tenant)
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName, Root));
        services.AddIdentityCore<ApplicationUser>(options => options.Password.RequiredLength = 8)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.RemoveAll(typeof(RealEstateCRM.Application.Common.Interfaces.ICurrentTenantService));
        services.AddSingleton<RealEstateCRM.Application.Common.Interfaces.ICurrentTenantService>(tenant);
        services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
        services.AddSingleton(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options);

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in Roles.All)
        {
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole<Guid>(role)).GetAwaiter().GetResult();
            }
        }

        return (db, userManager);
    }

    [Fact]
    public async Task CreateAsync_CreatesUser_WithRoleAndCompanyId()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        var (db, userManager) = CreateContext(dbName, tenant);
        var service = new UserManagementService(db, userManager, tenant);

        var created = await service.CreateAsync(new CreateUserRequest
        {
            FullName = "Agent Smith",
            Email = "agent.smith@test.local",
            Password = "SuperSecret1!",
            Role = Roles.SalesAgent
        });

        Assert.Equal("Agent Smith", created.FullName);
        Assert.Contains(Roles.SalesAgent, created.Roles);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task ListAsync_OnlyReturnsUsersInSameCompany()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyAId = Guid.NewGuid();
        var companyBId = Guid.NewGuid();

        var tenantA = new FakeCurrentTenantService { CompanyId = companyAId, UserId = Guid.NewGuid() };
        var (dbA, userManagerA) = CreateContext(dbName, tenantA);
        await new UserManagementService(dbA, userManagerA, tenantA).CreateAsync(new CreateUserRequest
        {
            FullName = "Company A Agent", Email = "a@test.local", Password = "SuperSecret1!", Role = Roles.SalesAgent
        });

        var tenantB = new FakeCurrentTenantService { CompanyId = companyBId, UserId = Guid.NewGuid() };
        var (dbB, userManagerB) = CreateContext(dbName, tenantB);
        await new UserManagementService(dbB, userManagerB, tenantB).CreateAsync(new CreateUserRequest
        {
            FullName = "Company B Agent", Email = "b@test.local", Password = "SuperSecret1!", Role = Roles.SalesAgent
        });

        var listA = await new UserManagementService(dbA, userManagerA, tenantA).ListAsync();

        var user = Assert.Single(listA);
        Assert.Equal("Company A Agent", user.FullName);
    }

    [Fact]
    public async Task UpdateActiveAsync_Fails_WhenUserNotInTenant()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new FakeCurrentTenantService { CompanyId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        var (db, userManager) = CreateContext(dbName, tenant);
        var service = new UserManagementService(db, userManager, tenant);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateActiveAsync(Guid.NewGuid(), new UpdateUserActiveRequest { IsActive = false }));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateRoleAsync_ReplacesExistingRole()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        var (db, userManager) = CreateContext(dbName, tenant);
        var service = new UserManagementService(db, userManager, tenant);

        var created = await service.CreateAsync(new CreateUserRequest
        {
            FullName = "Agent", Email = "role@test.local", Password = "SuperSecret1!", Role = Roles.SalesAgent
        });

        var updated = await service.UpdateRoleAsync(created.Id, new UpdateUserRoleRequest { Role = Roles.SalesManager });

        Assert.Contains(Roles.SalesManager, updated.Roles);
        Assert.DoesNotContain(Roles.SalesAgent, updated.Roles);
    }
}
