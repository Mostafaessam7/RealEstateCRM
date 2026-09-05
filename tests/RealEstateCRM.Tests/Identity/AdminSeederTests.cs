using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Identity;

/// <summary>
/// Covers the bootstrap account.
///
/// This is the gap these tests exist for: role seeding created four roles and no users, there is no
/// self-registration, and <c>POST /api/users</c> requires an admin — so creating the first user
/// required already being one. A deployed instance had zero users and no route to a first one, and
/// nothing in the suite noticed, because every other test constructs its users directly.
///
/// The two properties worth pinning are opposites of each other: Development must be trivially
/// usable, and everywhere else must refuse to invent a password.
/// </summary>
public class AdminSeederTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static IServiceProvider CreateProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName, Root));
        services.AddIdentityCore<ApplicationUser>(o => o.Password.RequiredLength = 8)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.RemoveAll(typeof(ICurrentTenantService));
        services.AddSingleton<ICurrentTenantService>(new FakeCurrentTenantService());
        services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
        services.AddSingleton(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName, Root).Options);

        var provider = services.BuildServiceProvider();

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
        {
            roleManager.CreateAsync(new IdentityRole<Guid>(role)).GetAwaiter().GetResult();
        }

        return provider;
    }

    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(
            values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value))).Build();

    [Fact]
    public async Task Development_creates_a_usable_SuperAdmin_with_the_documented_default()
    {
        var provider = CreateProvider(Guid.NewGuid().ToString());

        await AdminSeeder.SeedAdminAsync(provider, Config(), isDevelopment: true);

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(AdminSeeder.DevelopmentEmail);

        Assert.NotNull(admin);
        // The password must actually work — creating the account and getting the hash wrong would
        // look identical from the outside and still leave nobody able to sign in.
        Assert.True(await userManager.CheckPasswordAsync(admin!, AdminSeeder.DevelopmentPassword));
        Assert.Contains(Roles.SuperAdmin, await userManager.GetRolesAsync(admin!));
        // Null CompanyId is what marks a platform-level admin rather than a company user.
        Assert.Null(admin!.CompanyId);
        Assert.True(admin.IsActive);
    }

    [Fact]
    public async Task Outside_development_it_refuses_to_invent_a_password()
    {
        var provider = CreateProvider(Guid.NewGuid().ToString());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => AdminSeeder.SeedAdminAsync(provider, Config(), isDevelopment: false));

        // Throwing beats skipping: --init is an explicit deployment step, so a failure is visible,
        // whereas a silent skip hands someone a deployment they cannot log into and no reason why.
        Assert.Contains("Seed:AdminPassword", ex.Message);
        Assert.Empty(provider.GetRequiredService<UserManager<ApplicationUser>>().Users);
    }

    [Fact]
    public async Task Outside_development_a_configured_password_is_used()
    {
        var provider = CreateProvider(Guid.NewGuid().ToString());
        var config = Config(
            ("Seed:AdminEmail", "ops@example.com"),
            ("Seed:AdminPassword", "S0me-Real-Secret!"));

        await AdminSeeder.SeedAdminAsync(provider, config, isDevelopment: false);

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync("ops@example.com");

        Assert.NotNull(admin);
        Assert.True(await userManager.CheckPasswordAsync(admin!, "S0me-Real-Secret!"));
        Assert.False(await userManager.CheckPasswordAsync(admin!, AdminSeeder.DevelopmentPassword));
    }

    [Fact]
    public async Task It_is_bootstrap_only_and_leaves_an_existing_system_alone()
    {
        var dbName = Guid.NewGuid().ToString();
        var provider = CreateProvider(dbName);
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        await userManager.CreateAsync(new ApplicationUser
        {
            UserName = "someone@example.com",
            Email = "someone@example.com",
            FullName = "Someone",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        }, "Another-Password1!");

        await AdminSeeder.SeedAdminAsync(provider, Config(), isDevelopment: true);

        // Re-running --init after real accounts exist must not resurrect a default admin that
        // someone deliberately removed.
        Assert.Single(userManager.Users);
        Assert.Null(await userManager.FindByEmailAsync(AdminSeeder.DevelopmentEmail));
    }

    [Fact]
    public async Task Running_it_twice_does_not_create_a_second_admin()
    {
        var provider = CreateProvider(Guid.NewGuid().ToString());

        await AdminSeeder.SeedAdminAsync(provider, Config(), isDevelopment: true);
        await AdminSeeder.SeedAdminAsync(provider, Config(), isDevelopment: true);

        Assert.Single(provider.GetRequiredService<UserManager<ApplicationUser>>().Users);
    }
}
