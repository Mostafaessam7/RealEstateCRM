using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Deals.DTOs;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Application.Projects.DTOs;
using RealEstateCRM.Application.Units.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Deals;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Projects;
using RealEstateCRM.Infrastructure.Units;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Deals;

public class DealServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    private static async Task<(Guid leadId, Guid unitId)> SeedLeadAndUnitAsync(ApplicationDbContext db, FakeCurrentTenantService tenant)
    {
        var lead = await new LeadService(db, tenant, new NoOpNotificationService()).CreateAsync(new CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website });
        var project = await new ProjectService(db, tenant).CreateAsync(new CreateProjectRequest { Name = "Project " + Guid.NewGuid() });
        var unit = await new UnitService(db, tenant, new InMemoryCacheService()).CreateAsync(new CreateUnitRequest
        {
            ProjectId = project.Id,
            UnitCode = "U-" + Guid.NewGuid().ToString("N")[..6],
            Price = 1_000_000
        });
        return (lead.Id, unit.Id);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenUnitNotAvailable()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = agentId, Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);
        db.Users.Add(new ApplicationUser
        {
            Id = agentId, CompanyId = companyId, FullName = "Self", Email = "self@test.local",
            NormalizedEmail = "SELF@TEST.LOCAL", UserName = "self@test.local", NormalizedUserName = "SELF@TEST.LOCAL", IsActive = true
        });
        await db.SaveChangesAsync();

        var (leadId, unitId) = await SeedLeadAndUnitAsync(db, tenant);
        var dealService = new DealService(db, tenant, new NoOpNotificationService(), new InMemoryCacheService());

        // First deal reserves the unit.
        var deal1 = await dealService.CreateAsync(new CreateDealRequest { LeadId = leadId, UnitId = unitId, DealValue = 1_000_000 });
        await dealService.ReserveAsync(deal1.Id);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            dealService.CreateAsync(new CreateDealRequest { LeadId = leadId, UnitId = unitId, DealValue = 1_000_000 }));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task FullWorkflow_ReserveThenContract_UpdatesUnitStatusEachStep()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = agentId, Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);
        db.Users.Add(new ApplicationUser
        {
            Id = agentId, CompanyId = companyId, FullName = "Self", Email = "self2@test.local",
            NormalizedEmail = "SELF2@TEST.LOCAL", UserName = "self2@test.local", NormalizedUserName = "SELF2@TEST.LOCAL", IsActive = true
        });
        await db.SaveChangesAsync();

        var (leadId, unitId) = await SeedLeadAndUnitAsync(db, tenant);
        var notificationService = new NoOpNotificationService();
        var dealService = new DealService(db, tenant, notificationService, new InMemoryCacheService());
        var unitService = new UnitService(db, tenant, new InMemoryCacheService());

        var deal = await dealService.CreateAsync(new CreateDealRequest { LeadId = leadId, UnitId = unitId, DealValue = 1_000_000 });
        Assert.Equal(DealStatus.Pending, deal.Status);
        Assert.Equal(UnitStatus.Available, (await unitService.GetByIdAsync(unitId)).Status);

        var reserved = await dealService.ReserveAsync(deal.Id);
        Assert.Equal(DealStatus.Reserved, reserved.Status);
        Assert.NotNull(reserved.ReservationDate);
        Assert.Equal(UnitStatus.Reserved, (await unitService.GetByIdAsync(unitId)).Status);
        Assert.Contains(notificationService.Sent, n => n.UserId == agentId && n.Type == "DealReserved");

        var contracted = await dealService.ContractAsync(deal.Id);
        Assert.Equal(DealStatus.Contracted, contracted.Status);
        Assert.NotNull(contracted.ContractDate);
        Assert.Equal(UnitStatus.Sold, (await unitService.GetByIdAsync(unitId)).Status);
        Assert.Contains(notificationService.Sent, n => n.UserId == agentId && n.Type == "DealContracted");
    }

    [Fact]
    public async Task ReserveAsync_ThrowsConflict_WhenUnitWasConcurrentlyModified()
    {
        // Reproduces the double-booking race this fixes: two requests (two DbContexts, as in
        // two real concurrent HTTP requests) both read the unit as Available before either
        // writes. Simulated deterministically: db1 loads (and tracks) the unit first, then a
        // second, independent write goes through db2 and actually commits — so when db1's
        // ReserveAsync finally saves, its tracked original UpdatedAt is stale even though its
        // in-memory Status still reads Available. Without the concurrency token on
        // Unit.UpdatedAt (see UnitConfiguration), this would have silently double-reserved the
        // same unit for two different deals.
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = agentId, Roles = { Roles.SalesAgent } };
        await using var db1 = CreateDb(dbName, tenant);
        db1.Users.Add(new ApplicationUser
        {
            Id = agentId, CompanyId = companyId, FullName = "Self", Email = "race@test.local",
            NormalizedEmail = "RACE@TEST.LOCAL", UserName = "race@test.local", NormalizedUserName = "RACE@TEST.LOCAL", IsActive = true
        });
        await db1.SaveChangesAsync();

        var (leadId, unitId) = await SeedLeadAndUnitAsync(db1, tenant);
        var dealService1 = new DealService(db1, tenant, new NoOpNotificationService(), new InMemoryCacheService());
        var deal1 = await dealService1.CreateAsync(new CreateDealRequest { LeadId = leadId, UnitId = unitId, DealValue = 1_000_000 });

        // db1 tracks the unit now, seeing it as Available — simulates request 1 having already
        // read the unit before request 2 (on db2) commits its own change to it.
        await db1.Units.FirstOrDefaultAsync(u => u.Id == unitId);

        await using var db2 = CreateDb(dbName, tenant);
        var unitViaDb2 = await db2.Units.FirstOrDefaultAsync(u => u.Id == unitId);
        Assert.NotNull(unitViaDb2);
        unitViaDb2!.Status = UnitStatus.Reserved;
        unitViaDb2.UpdatedAt = DateTime.UtcNow;
        await db2.SaveChangesAsync(); // "request 2" wins the race and commits first.

        var ex = await Assert.ThrowsAsync<AppException>(() => dealService1.ReserveAsync(deal1.Id));

        Assert.Equal(409, ex.StatusCode);
        Assert.Contains("updated by someone else", ex.Message);
    }

    [Fact]
    public async Task CancelAsync_FromReserved_RevertsUnitToAvailable()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = agentId, Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);
        db.Users.Add(new ApplicationUser
        {
            Id = agentId, CompanyId = companyId, FullName = "Self", Email = "self3@test.local",
            NormalizedEmail = "SELF3@TEST.LOCAL", UserName = "self3@test.local", NormalizedUserName = "SELF3@TEST.LOCAL", IsActive = true
        });
        await db.SaveChangesAsync();

        var (leadId, unitId) = await SeedLeadAndUnitAsync(db, tenant);
        var notificationService = new NoOpNotificationService();
        var dealService = new DealService(db, tenant, notificationService, new InMemoryCacheService());
        var unitService = new UnitService(db, tenant, new InMemoryCacheService());

        var deal = await dealService.CreateAsync(new CreateDealRequest { LeadId = leadId, UnitId = unitId, DealValue = 1_000_000 });
        await dealService.ReserveAsync(deal.Id);

        var cancelled = await dealService.CancelAsync(deal.Id);
        Assert.Equal(DealStatus.Cancelled, cancelled.Status);
        Assert.Equal(UnitStatus.Available, (await unitService.GetByIdAsync(unitId)).Status);
        Assert.Contains(notificationService.Sent, n => n.UserId == agentId && n.Type == "DealCancelled");
    }

    [Fact]
    public async Task CancelAsync_Fails_WhenAlreadyContracted()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = agentId, Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);
        db.Users.Add(new ApplicationUser
        {
            Id = agentId, CompanyId = companyId, FullName = "Self", Email = "self4@test.local",
            NormalizedEmail = "SELF4@TEST.LOCAL", UserName = "self4@test.local", NormalizedUserName = "SELF4@TEST.LOCAL", IsActive = true
        });
        await db.SaveChangesAsync();

        var (leadId, unitId) = await SeedLeadAndUnitAsync(db, tenant);
        var dealService = new DealService(db, tenant, new NoOpNotificationService(), new InMemoryCacheService());

        var deal = await dealService.CreateAsync(new CreateDealRequest { LeadId = leadId, UnitId = unitId, DealValue = 1_000_000 });
        await dealService.ReserveAsync(deal.Id);
        await dealService.ContractAsync(deal.Id);

        var ex = await Assert.ThrowsAsync<AppException>(() => dealService.CancelAsync(deal.Id));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task SalesAgent_CannotManage_AnotherAgentsDeal()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var ownerAgentId = Guid.NewGuid();
        var otherAgentId = Guid.NewGuid();

        var ownerTenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = ownerAgentId, Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, ownerTenant);
        db.Users.Add(new ApplicationUser
        {
            Id = ownerAgentId, CompanyId = companyId, FullName = "Owner", Email = "owner@test.local",
            NormalizedEmail = "OWNER@TEST.LOCAL", UserName = "owner@test.local", NormalizedUserName = "OWNER@TEST.LOCAL", IsActive = true
        });
        db.Users.Add(new ApplicationUser
        {
            Id = otherAgentId, CompanyId = companyId, FullName = "Other", Email = "other@test.local",
            NormalizedEmail = "OTHER@TEST.LOCAL", UserName = "other@test.local", NormalizedUserName = "OTHER@TEST.LOCAL", IsActive = true
        });
        await db.SaveChangesAsync();

        var (leadId, unitId) = await SeedLeadAndUnitAsync(db, ownerTenant);
        var ownerDealService = new DealService(db, ownerTenant, new NoOpNotificationService(), new InMemoryCacheService());
        var deal = await ownerDealService.CreateAsync(new CreateDealRequest { LeadId = leadId, UnitId = unitId, DealValue = 1_000_000 });

        var otherTenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = otherAgentId, Roles = { Roles.SalesAgent } };
        var otherDealService = new DealService(db, otherTenant, new NoOpNotificationService(), new InMemoryCacheService());

        var ex = await Assert.ThrowsAsync<AppException>(() => otherDealService.ReserveAsync(deal.Id));
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task SalesAgent_CannotCreateDeal_AssignedToAnotherAgent()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var callerAgentId = Guid.NewGuid();
        var otherAgentId = Guid.NewGuid();

        var callerTenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = callerAgentId, Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, callerTenant);
        db.Users.Add(new ApplicationUser
        {
            Id = callerAgentId, CompanyId = companyId, FullName = "Caller", Email = "caller@test.local",
            NormalizedEmail = "CALLER@TEST.LOCAL", UserName = "caller@test.local", NormalizedUserName = "CALLER@TEST.LOCAL", IsActive = true
        });
        db.Users.Add(new ApplicationUser
        {
            Id = otherAgentId, CompanyId = companyId, FullName = "Other", Email = "other2@test.local",
            NormalizedEmail = "OTHER2@TEST.LOCAL", UserName = "other2@test.local", NormalizedUserName = "OTHER2@TEST.LOCAL", IsActive = true
        });
        await db.SaveChangesAsync();

        var (leadId, unitId) = await SeedLeadAndUnitAsync(db, callerTenant);
        var dealService = new DealService(db, callerTenant, new NoOpNotificationService(), new InMemoryCacheService());

        var ex = await Assert.ThrowsAsync<AppException>(() => dealService.CreateAsync(new CreateDealRequest
        {
            LeadId = leadId, UnitId = unitId, SalesAgentId = otherAgentId, DealValue = 1_000_000
        }));

        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task CompanyAdmin_CanManage_AnyAgentsDeal()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var agentTenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = agentId, Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, agentTenant);
        db.Users.Add(new ApplicationUser
        {
            Id = agentId, CompanyId = companyId, FullName = "Agent", Email = "agent5@test.local",
            NormalizedEmail = "AGENT5@TEST.LOCAL", UserName = "agent5@test.local", NormalizedUserName = "AGENT5@TEST.LOCAL", IsActive = true
        });
        await db.SaveChangesAsync();

        var (leadId, unitId) = await SeedLeadAndUnitAsync(db, agentTenant);
        var deal = await new DealService(db, agentTenant, new NoOpNotificationService(), new InMemoryCacheService()).CreateAsync(new CreateDealRequest
        {
            LeadId = leadId, UnitId = unitId, SalesAgentId = agentId, DealValue = 1_000_000
        });

        var adminTenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        var adminDealService = new DealService(db, adminTenant, new NoOpNotificationService(), new InMemoryCacheService());

        var reserved = await adminDealService.ReserveAsync(deal.Id);
        Assert.Equal(DealStatus.Reserved, reserved.Status);
    }
}
