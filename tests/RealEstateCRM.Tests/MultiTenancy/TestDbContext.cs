using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Tests.MultiTenancy;

/// <summary>
/// Wires the same TenantModelBuilderExtensions/TenantSaveChangesExtensions used by the
/// real ApplicationDbContext, against a minimal model, so the isolation mechanism itself
/// is under test rather than a reimplementation of it.
/// </summary>
internal class TestDbContext : DbContext, ITenantScopedDbContext
{
    private readonly ICurrentTenantService _currentTenant;

    public TestDbContext(DbContextOptions<TestDbContext> options, ICurrentTenantService currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<TestTenantEntity> Items => Set<TestTenantEntity>();

    public Guid? CurrentCompanyId => _currentTenant.CompanyId;

    public IQueryable<TestTenantEntity> AllTenantsItems() => Set<TestTenantEntity>().IgnoreQueryFilters();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestTenantEntity>().HasKey(e => e.Id);
        modelBuilder.ApplyGlobalQueryFilters(this);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.ApplyTenantOnAdd(_currentTenant);
        return base.SaveChangesAsync(cancellationToken);
    }
}
