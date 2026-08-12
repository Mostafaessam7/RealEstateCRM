using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RealEstateCRM.Application.Auth.DTOs;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Auth;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Persistence;
using Xunit;

namespace RealEstateCRM.Tests.Auth;

/// <summary>
/// AuthService had zero direct tests before this — the most security-critical service in the
/// app (login, refresh rotation, logout, change/forgot/reset password). Uses a real
/// UserManager against an in-memory EF DB (mirrors UserManagementServiceTests' pattern) so
/// password hashing/verification and Identity token generation are exercised for real, not
/// mocked away.
/// </summary>
public class AuthServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private class FakeEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = new();

        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            Sent.Add((to, subject, body));
            return Task.CompletedTask;
        }
    }

    private static (ApplicationDbContext db, UserManager<ApplicationUser> userManager, AuthService authService, FakeEmailSender emailSender) CreateContext(string dbName)
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddDataProtection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName, Root));
        services.AddIdentityCore<ApplicationUser>(options => options.Password.RequiredLength = 8)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.RemoveAll(typeof(ICurrentTenantService));
        services.AddSingleton<ICurrentTenantService>(new RealEstateCRM.Tests.MultiTenancy.FakeCurrentTenantService());
        services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
        services.AddSingleton(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options);

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        var jwtOptions = new JwtOptions
        {
            Key = "unit-test-signing-key-at-least-32-chars-long!",
            Issuer = "RealEstateCRM.Tests",
            Audience = "RealEstateCRM.Tests",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7,
        };
        var jwtGenerator = new JwtTokenGenerator(Options.Create(jwtOptions));
        var emailSender = new FakeEmailSender();

        var authService = new AuthService(userManager, db, jwtGenerator, emailSender, Options.Create(jwtOptions));
        return (db, userManager, authService, emailSender);
    }

    private static async Task<(ApplicationUser user, ApplicationDbContext db, UserManager<ApplicationUser> userManager, AuthService authService, FakeEmailSender emailSender)> CreateActiveUserAsync(
        string dbName, string password = "OriginalPass1!", bool companyActive = true)
    {
        var (db, userManager, authService, emailSender) = CreateContext(dbName);

        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company { Id = companyId, Name = "Test Co", Slug = "test-co-" + Guid.NewGuid(), IsActive = companyActive, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "agent@test.local",
            Email = "agent@test.local",
            CompanyId = companyId,
            FullName = "Test Agent",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, string.Join(",", createResult.Errors.Select(e => e.Description)));

        return (user, db, userManager, authService, emailSender);
    }

    [Fact]
    public async Task LoginAsync_Succeeds_WithCorrectCredentials()
    {
        var dbName = Guid.NewGuid().ToString();
        var (user, _, _, authService, _) = await CreateActiveUserAsync(dbName);

        var response = await authService.LoginAsync(new LoginRequest { Email = user.Email!, Password = "OriginalPass1!" }, "127.0.0.1");

        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
    }

    [Fact]
    public async Task LoginAsync_Throws401_WithWrongPassword()
    {
        var dbName = Guid.NewGuid().ToString();
        var (user, _, _, authService, _) = await CreateActiveUserAsync(dbName);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            authService.LoginAsync(new LoginRequest { Email = user.Email!, Password = "WrongPassword1!" }, "127.0.0.1"));

        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_Throws401_ForUnknownEmail_WithSameMessageAsWrongPassword()
    {
        // Both branches must be indistinguishable to the caller — otherwise login becomes an
        // account-enumeration oracle.
        var dbName = Guid.NewGuid().ToString();
        var (user, _, _, authService, _) = await CreateActiveUserAsync(dbName);

        var wrongPasswordEx = await Assert.ThrowsAsync<AppException>(() =>
            authService.LoginAsync(new LoginRequest { Email = user.Email!, Password = "WrongPassword1!" }, "127.0.0.1"));
        var unknownEmailEx = await Assert.ThrowsAsync<AppException>(() =>
            authService.LoginAsync(new LoginRequest { Email = "nobody@test.local", Password = "WhoKnows1!" }, "127.0.0.1"));

        Assert.Equal(wrongPasswordEx.Message, unknownEmailEx.Message);
        Assert.Equal(401, unknownEmailEx.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_Throws401_WhenUserIsInactive()
    {
        var dbName = Guid.NewGuid().ToString();
        var (user, db, userManager, authService, _) = await CreateActiveUserAsync(dbName);
        user.IsActive = false;
        await userManager.UpdateAsync(user);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            authService.LoginAsync(new LoginRequest { Email = user.Email!, Password = "OriginalPass1!" }, "127.0.0.1"));

        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_Throws401_WhenCompanyIsInactive()
    {
        var dbName = Guid.NewGuid().ToString();
        var (user, _, _, authService, _) = await CreateActiveUserAsync(dbName, companyActive: false);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            authService.LoginAsync(new LoginRequest { Email = user.Email!, Password = "OriginalPass1!" }, "127.0.0.1"));

        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task RefreshAsync_RotatesToken_AndRevokesThePrevious()
    {
        var dbName = Guid.NewGuid().ToString();
        var (user, db, _, authService, _) = await CreateActiveUserAsync(dbName);
        var login = await authService.LoginAsync(new LoginRequest { Email = user.Email!, Password = "OriginalPass1!" }, "127.0.0.1");

        var refreshed = await authService.RefreshAsync(login.RefreshToken, "127.0.0.1");

        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        // The old token must no longer work.
        await Assert.ThrowsAsync<AppException>(() => authService.RefreshAsync(login.RefreshToken, "127.0.0.1"));
    }

    [Fact]
    public async Task RefreshAsync_Throws401_ForUnknownToken()
    {
        var dbName = Guid.NewGuid().ToString();
        var (_, _, _, authService, _) = await CreateActiveUserAsync(dbName);

        var ex = await Assert.ThrowsAsync<AppException>(() => authService.RefreshAsync("not-a-real-token", "127.0.0.1"));
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task LogoutAsync_RevokesToken_SoItCanNoLongerRefresh()
    {
        var dbName = Guid.NewGuid().ToString();
        var (user, _, _, authService, _) = await CreateActiveUserAsync(dbName);
        var login = await authService.LoginAsync(new LoginRequest { Email = user.Email!, Password = "OriginalPass1!" }, "127.0.0.1");

        await authService.LogoutAsync(login.RefreshToken);

        await Assert.ThrowsAsync<AppException>(() => authService.RefreshAsync(login.RefreshToken, "127.0.0.1"));
    }

    [Fact]
    public async Task ChangePasswordAsync_RevokesAllActiveRefreshTokens()
    {
        var dbName = Guid.NewGuid().ToString();
        var (user, db, _, authService, _) = await CreateActiveUserAsync(dbName);
        var session1 = await authService.LoginAsync(new LoginRequest { Email = user.Email!, Password = "OriginalPass1!" }, "device-1");
        var session2 = await authService.LoginAsync(new LoginRequest { Email = user.Email!, Password = "OriginalPass1!" }, "device-2");

        await authService.ChangePasswordAsync(user.Id, new ChangePasswordRequest { CurrentPassword = "OriginalPass1!", NewPassword = "NewPassword1!" });

        // Both sessions' refresh tokens — not just the one used to authenticate this call —
        // must be dead, since a stolen token on another device must not survive the change.
        await Assert.ThrowsAsync<AppException>(() => authService.RefreshAsync(session1.RefreshToken, "device-1"));
        await Assert.ThrowsAsync<AppException>(() => authService.RefreshAsync(session2.RefreshToken, "device-2"));

        // And the new password actually works.
        var relogin = await authService.LoginAsync(new LoginRequest { Email = user.Email!, Password = "NewPassword1!" }, "device-1");
        Assert.False(string.IsNullOrWhiteSpace(relogin.AccessToken));
    }

    [Fact]
    public async Task ChangePasswordAsync_Throws400_WithWrongCurrentPassword()
    {
        var dbName = Guid.NewGuid().ToString();
        var (user, _, _, authService, _) = await CreateActiveUserAsync(dbName);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            authService.ChangePasswordAsync(user.Id, new ChangePasswordRequest { CurrentPassword = "WrongOne1!", NewPassword = "NewPassword1!" }));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task ForgotPasswordAsync_DoesNotThrow_ForUnknownEmail_AndSendsNoEmail()
    {
        // Must never reveal whether an account exists — no exception, no email sent, same
        // observable outcome as a known email that's simply inactive.
        var dbName = Guid.NewGuid().ToString();
        var (_, _, _, authService, emailSender) = await CreateActiveUserAsync(dbName);

        var exception = await Record.ExceptionAsync(() => authService.ForgotPasswordAsync("nobody@test.local"));

        Assert.Null(exception);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ForgotPasswordAsync_SendsResetEmail_ForKnownActiveUser()
    {
        var dbName = Guid.NewGuid().ToString();
        var (user, _, _, authService, emailSender) = await CreateActiveUserAsync(dbName);

        await authService.ForgotPasswordAsync(user.Email!);

        Assert.Single(emailSender.Sent);
        Assert.Equal(user.Email, emailSender.Sent[0].To);
    }

    [Fact]
    public async Task ResetPasswordAsync_ChangesPassword_AndRevokesAllActiveRefreshTokens()
    {
        var dbName = Guid.NewGuid().ToString();
        var (user, db, userManager, authService, _) = await CreateActiveUserAsync(dbName);
        var session = await authService.LoginAsync(new LoginRequest { Email = user.Email!, Password = "OriginalPass1!" }, "device-1");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await authService.ResetPasswordAsync(new ResetPasswordRequest { Email = user.Email!, Token = token, NewPassword = "ResetPassword1!" });

        await Assert.ThrowsAsync<AppException>(() => authService.RefreshAsync(session.RefreshToken, "device-1"));

        var relogin = await authService.LoginAsync(new LoginRequest { Email = user.Email!, Password = "ResetPassword1!" }, "device-1");
        Assert.False(string.IsNullOrWhiteSpace(relogin.AccessToken));
    }

    [Fact]
    public async Task ResetPasswordAsync_Throws400_WithInvalidToken()
    {
        var dbName = Guid.NewGuid().ToString();
        var (user, _, _, authService, _) = await CreateActiveUserAsync(dbName);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            authService.ResetPasswordAsync(new ResetPasswordRequest { Email = user.Email!, Token = "garbage-token", NewPassword = "ResetPassword1!" }));

        Assert.Equal(400, ex.StatusCode);
    }
}
