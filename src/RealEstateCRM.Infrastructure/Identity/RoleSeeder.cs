using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RealEstateCRM.Domain.Constants;

namespace RealEstateCRM.Infrastructure.Identity;

public static class RoleSeeder
{
    /// <summary>
    /// Ensures every role in <see cref="Roles.All"/> exists. Safe to run concurrently from more
    /// than one instance, and safe to run repeatedly.
    /// </summary>
    /// <remarks>
    /// The previous version was a check-then-act race:
    /// <code>
    /// if (!await roleManager.RoleExistsAsync(name))
    ///     await roleManager.CreateAsync(new IdentityRole&lt;Guid&gt;(name));
    /// </code>
    /// Two instances starting together both observe "does not exist", both insert, and the one
    /// that commits second violates the unique index on the normalized role name. That surfaced as
    /// a failed start rather than a benign no-op, because this ran unguarded during startup.
    ///
    /// The fix is not a lock. Locking across instances needs shared infrastructure and still leaves
    /// the failure possible if it is ever bypassed. Instead this converges on the desired end
    /// state: losing the race is treated as success, because the other instance produced exactly
    /// the row this one wanted. The check is kept only as a fast path to avoid pointless inserts.
    ///
    /// Two distinct failure shapes have to be absorbed, and which one occurs depends on where the
    /// collision is detected:
    ///   - the store surfaces the unique-index violation as a <see cref="DbUpdateException"/>;
    ///   - Identity detects the duplicate itself first and returns a failed
    ///     <see cref="IdentityResult"/> carrying <c>DuplicateRoleName</c>.
    /// Both are only forgiven once the role is confirmed present. A create that failed for any
    /// other reason still throws, so a genuinely broken seed is not silently swallowed.
    /// </remarks>
    public static async Task SeedRolesAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in Roles.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureRoleAsync(roleManager, roleName);
        }
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        // Fast path only. A false negative here is fine - the create below is what actually has to
        // be correct under concurrency.
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        try
        {
            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            if (result.Succeeded)
            {
                return;
            }

            // A concurrent creator can also be reported by Identity rather than by the database.
            if (await roleManager.RoleExistsAsync(roleName))
            {
                return;
            }

            var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException(
                $"Could not create role '{roleName}', and it does not exist. Errors: {errors}");
        }
        catch (DbUpdateException)
        {
            // Written as a catch body rather than a `when` filter because C# forbids awaiting in a
            // filter expression (CS7094), and confirming the role exists requires a round trip.
            if (await roleManager.RoleExistsAsync(roleName))
            {
                // Another instance committed the same role first. That is the outcome this wanted.
                return;
            }

            // The insert failed for some other reason - surface it.
            throw;
        }
    }
}
