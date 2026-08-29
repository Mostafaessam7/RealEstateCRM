using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Tests.Identity;

/// <summary>
/// Covers the case the old seeder could not survive: two instances starting at the same time.
///
/// The previous implementation was check-then-act - ask whether the role exists, then create it if
/// not. Both instances observe "does not exist", both insert, and whichever commits second hits the
/// unique index on the normalized role name. Because this ran inline during startup, that surfaced
/// as a failed start rather than a retryable job.
///
/// A real concurrent test would need a real database with a real unique index; the suite runs on
/// EF Core InMemory, which does not enforce one. So instead of trying to provoke the collision by
/// timing - which would be slow and flaky - these drive the store into each shape the collision
/// actually produces, and assert the seeder converges instead of throwing.
/// </summary>
public class RoleSeederConcurrencyTests
{
    [Fact]
    public async Task Seeding_creates_every_role_when_none_exist()
    {
        var store = new FakeRoleStore();
        await using var provider = BuildProvider(store);

        await RoleSeeder.SeedRolesAsync(provider);

        Assert.Equal(Roles.All.OrderBy(r => r), store.RoleNames.OrderBy(r => r));
    }

    [Fact]
    public async Task Seeding_twice_is_idempotent_and_does_not_duplicate()
    {
        var store = new FakeRoleStore();
        await using var provider = BuildProvider(store);

        await RoleSeeder.SeedRolesAsync(provider);
        await RoleSeeder.SeedRolesAsync(provider);

        Assert.Equal(Roles.All.Length, store.RoleNames.Count);
        // The second pass must not have re-inserted anything.
        Assert.Equal(Roles.All.Length, store.CreateAttempts);
    }

    [Fact]
    public async Task Losing_the_race_at_the_database_is_treated_as_success()
    {
        // Every insert throws the unique-index violation, as if another instance committed first -
        // and the row is present afterwards, which is what makes it a lost race rather than a fault.
        var store = new FakeRoleStore { FailEveryCreateAsRaceLost = true };
        await using var provider = BuildProvider(store);

        var exception = await Record.ExceptionAsync(() => RoleSeeder.SeedRolesAsync(provider));

        Assert.Null(exception);
        Assert.Equal(Roles.All.OrderBy(r => r), store.RoleNames.OrderBy(r => r));
    }

    [Fact]
    public async Task A_create_that_fails_for_any_other_reason_still_throws()
    {
        // The forgiving path must not become a blanket catch. Here the insert fails and the role is
        // genuinely absent afterwards, so seeding has not achieved what it claims and must say so.
        var store = new FakeRoleStore { FailEveryCreateWithoutPersisting = true };
        await using var provider = BuildProvider(store);

        var exception = await Record.ExceptionAsync(() => RoleSeeder.SeedRolesAsync(provider));

        Assert.NotNull(exception);
        Assert.Empty(store.RoleNames);
    }

    [Fact]
    public async Task Concurrent_seeders_all_complete_when_they_collide_on_the_same_role()
    {
        // The real scenario: N instances start together, all check, all find nothing, all insert.
        //
        // Left to chance this proves nothing - an earlier version of this test passed against the
        // buggy seeder, because the fake store never yielded so each seeder ran start to finish
        // before the next began, and no two ever collided. So the collision is forced rather than
        // hoped for: HoldFirstCreateUntilAllArrive makes every seeder block inside CreateAsync for
        // the first role until all of them are inside it, which guarantees each one passed the
        // existence check while the row was still absent.
        const int instances = 8;
        var store = new FakeRoleStore
        {
            RejectDuplicateCreates = true,
            HoldFirstCreateUntilAllArrive = instances
        };

        var seeders = Enumerable.Range(0, instances).Select(async _ =>
        {
            await using var provider = BuildProvider(store);
            await RoleSeeder.SeedRolesAsync(provider);
        });

        var exception = await Record.ExceptionAsync(() => Task.WhenAll(seeders));

        Assert.Null(exception);
        Assert.Equal(Roles.All.OrderBy(r => r), store.RoleNames.OrderBy(r => r));

        // Exactly one insert won per role; the rest were rejected by the unique index and forgiven.
        Assert.Equal(Roles.All.Length, store.CommittedCreates);

        // And the collision really happened - otherwise this test is green for the wrong reason.
        Assert.True(
            store.RejectedDuplicates >= instances - 1,
            $"Expected at least {instances - 1} rejected duplicate inserts, saw {store.RejectedDuplicates}. " +
            "Without real contention this test would pass even against a check-then-act seeder.");
    }

    private static ServiceProvider BuildProvider(IRoleStore<IdentityRole<Guid>> store)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(store);
        services.AddSingleton<IRoleValidator<IdentityRole<Guid>>>(new RoleValidator<IdentityRole<Guid>>());
        services.AddSingleton<ILookupNormalizer>(new UpperInvariantLookupNormalizer());
        services.AddSingleton(new IdentityErrorDescriber());
        services.AddSingleton(Options.Create(new IdentityOptions()));
        services.AddSingleton(sp => new RoleManager<IdentityRole<Guid>>(
            sp.GetRequiredService<IRoleStore<IdentityRole<Guid>>>(),
            sp.GetServices<IRoleValidator<IdentityRole<Guid>>>(),
            sp.GetRequiredService<ILookupNormalizer>(),
            sp.GetRequiredService<IdentityErrorDescriber>(),
            NullLogger<RoleManager<IdentityRole<Guid>>>.Instance));

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Minimal in-memory role store whose failure modes can be steered. Hand-written rather than
    /// mocked so the assertions describe database behaviour (unique index, commit ordering) instead
    /// of describing a mocking framework's call log.
    /// </summary>
    private sealed class FakeRoleStore : IRoleStore<IdentityRole<Guid>>
    {
        private readonly Dictionary<string, IdentityRole<Guid>> _byNormalizedName = new(StringComparer.Ordinal);
        private readonly object _gate = new();

        /// <summary>Insert throws, but the row lands anyway - a lost race.</summary>
        public bool FailEveryCreateAsRaceLost { get; init; }

        /// <summary>Insert throws and nothing is persisted - a genuine fault.</summary>
        public bool FailEveryCreateWithoutPersisting { get; init; }

        /// <summary>Behave like a unique index: a second insert of the same name is rejected.</summary>
        public bool RejectDuplicateCreates { get; init; }

        /// <summary>
        /// When set, the first role's insert blocks until this many callers are inside it. Forces
        /// the collision deterministically instead of relying on scheduler luck.
        /// </summary>
        public int HoldFirstCreateUntilAllArrive { get; init; }

        public int CreateAttempts;
        public int CommittedCreates;
        public int RejectedDuplicates;

        private readonly TaskCompletionSource _allArrived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrived;
        private volatile bool _barrierOpen;

        public IReadOnlyCollection<string> RoleNames
        {
            get { lock (_gate) { return _byNormalizedName.Values.Select(r => r.Name!).ToList(); } }
        }

        public async Task<IdentityResult> CreateAsync(IdentityRole<Guid> role, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref CreateAttempts);
            var key = role.NormalizedName ?? role.Name!.ToUpperInvariant();

            // Hold everyone at the first role's insert until they have all arrived, so each one is
            // guaranteed to have passed its existence check before any insert commits.
            if (HoldFirstCreateUntilAllArrive > 0 && !_barrierOpen)
            {
                if (Interlocked.Increment(ref _arrived) >= HoldFirstCreateUntilAllArrive)
                {
                    _barrierOpen = true;
                    _allArrived.TrySetResult();
                }

                await _allArrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }

            if (FailEveryCreateWithoutPersisting)
            {
                throw new DbUpdateException("Insert failed and nothing was written.");
            }

            if (FailEveryCreateAsRaceLost)
            {
                lock (_gate) { _byNormalizedName.TryAdd(key, role); }
                Interlocked.Increment(ref CommittedCreates);
                throw new DbUpdateException("Duplicate key on the unique index over NormalizedName.");
            }

            lock (_gate)
            {
                if (!_byNormalizedName.TryAdd(key, role))
                {
                    if (RejectDuplicateCreates)
                    {
                        Interlocked.Increment(ref RejectedDuplicates);
                        throw new DbUpdateException("Duplicate key on the unique index over NormalizedName.");
                    }

                    return IdentityResult.Success;
                }
            }

            Interlocked.Increment(ref CommittedCreates);
            return IdentityResult.Success;
        }

        public Task<IdentityRole<Guid>?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _byNormalizedName.TryGetValue(normalizedRoleName, out var role);
                return Task.FromResult(role);
            }
        }

        public Task<IdentityRole<Guid>?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                return Task.FromResult(_byNormalizedName.Values.FirstOrDefault(r => r.Id.ToString() == roleId));
            }
        }

        public Task<IdentityResult> UpdateAsync(IdentityRole<Guid> role, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(IdentityRole<Guid> role, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<string> GetRoleIdAsync(IdentityRole<Guid> role, CancellationToken cancellationToken) =>
            Task.FromResult(role.Id.ToString());

        public Task<string?> GetRoleNameAsync(IdentityRole<Guid> role, CancellationToken cancellationToken) =>
            Task.FromResult(role.Name);

        public Task SetRoleNameAsync(IdentityRole<Guid> role, string? roleName, CancellationToken cancellationToken)
        {
            role.Name = roleName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedRoleNameAsync(IdentityRole<Guid> role, CancellationToken cancellationToken) =>
            Task.FromResult(role.NormalizedName);

        public Task SetNormalizedRoleNameAsync(IdentityRole<Guid> role, string? normalizedName, CancellationToken cancellationToken)
        {
            role.NormalizedName = normalizedName;
            return Task.CompletedTask;
        }

        public void Dispose() { }
    }
}
