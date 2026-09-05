using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealEstateCRM.Domain.Constants;

namespace RealEstateCRM.Infrastructure.Identity;

/// <summary>
/// Creates the first SuperAdmin, so a fresh deployment can actually be signed into.
/// </summary>
/// <remarks>
/// Until this existed the system had no bootstrap path at all. <see cref="RoleSeeder"/> created the
/// four roles and stopped there, leaving zero users; there is no self-registration endpoint, and
/// <c>POST /api/users</c> is <c>[Authorize(Roles = CompanyAdmin,SuperAdmin)]</c> — so creating the
/// first user required being a user. A freshly deployed instance was unreachable through its own
/// front door, and the only way in was writing an Identity password hash straight into the table.
///
/// <para>
/// <b>Development</b> gets a well-known default so "clone, run, sign in" works. <b>Anywhere else</b>
/// requires <c>Seed:AdminPassword</c> to be set and throws without it. Throwing is deliberate: this
/// runs as the explicit <c>--init</c> deployment step, so a failure there is visible and fixable,
/// whereas skipping silently would hand someone a deployment they cannot log into and no
/// indication why. The same reasoning as Gym Manager's <c>Seed__AdminPassword</c>, which refuses to
/// start rather than ship a publicly-known privileged account.
/// </para>
///
/// <para>
/// Bootstrap only: if any user already exists this does nothing, so re-running <c>--init</c> after
/// real accounts are in place cannot resurrect a default admin someone deliberately removed.
/// </para>
/// </remarks>
public static class AdminSeeder
{
    /// <summary>Used only when the environment is Development and nothing is configured.</summary>
    public const string DevelopmentEmail = "admin@realestatecrm.local";

    /// <summary>Used only when the environment is Development and nothing is configured.</summary>
    public const string DevelopmentPassword = "Admin@12345";

    public static async Task SeedAdminAsync(
        IServiceProvider services,
        IConfiguration configuration,
        bool isDevelopment,
        CancellationToken cancellationToken = default)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var email = configuration["Seed:AdminEmail"] ?? DevelopmentEmail;
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(password))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException(
                    "No admin account can be created because Seed:AdminPassword is not set. " +
                    "Outside Development this is required: without it the deployment would have " +
                    "no users, no way to register, and no way to create one. Set " +
                    "Seed__AdminPassword (and optionally Seed__AdminEmail) and run --init again.");
            }

            password = DevelopmentPassword;
        }

        // Bootstrap only. Any existing user means the system is already reachable, and creating
        // another privileged account would be a surprise rather than a convenience.
        if (await userManager.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Administrator",
            IsActive = true,
            // Null marks a platform-level SuperAdmin rather than a user inside one company.
            CompanyId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var created = await userManager.CreateAsync(admin, password);

        if (!created.Succeeded)
        {
            // A duplicate here means another instance won the race between the check above and
            // this call, which is a success for our purposes: an admin exists.
            if (created.Errors.Any(e => e.Code == "DuplicateUserName" || e.Code == "DuplicateEmail"))
            {
                return;
            }

            throw new InvalidOperationException(
                "Could not create the initial admin account: " +
                string.Join("; ", created.Errors.Select(e => $"{e.Code} {e.Description}")));
        }

        var assigned = await userManager.AddToRoleAsync(admin, Roles.SuperAdmin);

        if (!assigned.Succeeded)
        {
            throw new InvalidOperationException(
                $"Created the admin account but could not put it in the {Roles.SuperAdmin} role: " +
                string.Join("; ", assigned.Errors.Select(e => $"{e.Code} {e.Description}")) +
                ". The account exists but has no permissions, which is worse than not existing.");
        }
    }
}
