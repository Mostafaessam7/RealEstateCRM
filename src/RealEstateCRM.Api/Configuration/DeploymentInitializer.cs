using Hangfire;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Jobs;

namespace RealEstateCRM.Api.Configuration;

/// <summary>
/// One-off deployment work: seeding roles and registering Hangfire's recurring jobs.
/// </summary>
/// <remarks>
/// Both of these used to run inline in <c>Program.cs</c> on every boot of every instance. Two
/// problems with that, and they are different problems:
///
/// <b>Seeding</b> mutates the database. Every instance doing it on start means N concurrent
/// writers on a cold deploy, and it happens before the app can serve traffic, so a failure is a
/// failed start rather than a failed job. <see cref="RoleSeeder"/> is now safe under concurrency
/// on its own, but the right shape is still that data changes are an explicit, observable step
/// someone runs and can see the result of - not a side effect of a process starting.
///
/// <b>Recurring job registration</b> is not a race in the same way - <c>AddOrUpdate</c> is
/// idempotent by design - but re-registering identical schedules on every restart is noise, and it
/// tied job definitions to the lifecycle of the request-serving host. Registering them here means
/// the schedule changes when someone deploys a schedule change, which is when it should change.
///
/// Invoked as a deployment step:
/// <code>dotnet RealEstateCRM.Api.dll --init</code>
/// The process runs the tasks and exits without listening. In Development it also runs on startup,
/// because locally there is exactly one instance and requiring a second command before the app is
/// usable is friction that gets worked around rather than followed.
/// </remarks>
public static class DeploymentInitializer
{
    /// <summary>CLI flag that requests init-and-exit.</summary>
    public const string Flag = "--init";

    /// <summary>
    /// True when the process was started to run deployment tasks rather than to serve traffic.
    /// </summary>
    public static bool IsRequested(string[] args) =>
        args is not null && args.Any(a => string.Equals(a, Flag, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// True when deployment tasks should also run as part of a normal startup. Development only:
    /// a single local instance cannot race itself, and this preserves the existing workflow of
    /// "run the app, it works".
    /// </summary>
    public static bool ShouldRunOnStartup(IHostEnvironment environment) =>
        environment.IsDevelopment();

    public static async Task RunAsync(IServiceProvider services, ILogger logger, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running deployment tasks.");

        using (var scope = services.CreateScope())
        {
            await RoleSeeder.SeedRolesAsync(scope.ServiceProvider, cancellationToken);
        }

        logger.LogInformation("Roles seeded.");

        RegisterRecurringJobs();

        logger.LogInformation("Recurring jobs registered. Deployment tasks complete.");
    }

    /// <summary>
    /// Registers the recurring jobs. Separate from <see cref="RunAsync"/> so the schedule
    /// definitions are one readable block rather than being buried in startup wiring.
    /// </summary>
    public static void RegisterRecurringJobs()
    {
        RecurringJob.AddOrUpdate<ReminderJobs>(
            "lead-follow-up-reminders",
            job => job.SendDueFollowUpRemindersAsync(CancellationToken.None),
            "*/5 * * * *");

        RecurringJob.AddOrUpdate<ReminderJobs>(
            "task-reminders",
            job => job.SendDueTaskRemindersAsync(CancellationToken.None),
            "*/5 * * * *");
    }
}
