using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Commissions.DTOs;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Deals.DTOs;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Application.Projects.DTOs;
using RealEstateCRM.Application.Units.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Commissions;
using RealEstateCRM.Infrastructure.Deals;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Projects;
using RealEstateCRM.Infrastructure.Units;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Commissions;

public class CommissionServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    private static async Task<(Guid dealId, Guid agentId)> SeedContractedDealAsync(
        ApplicationDbContext db, Guid companyId, FakeCurrentTenantService adminTenant)
    {
        var agentId = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = agentId, CompanyId = companyId, FullName = "Agent", Email = $"{agentId}@test.local",
            NormalizedEmail = $"{agentId}@test.local".ToUpperInvariant(), UserName = $"{agentId}@test.local",
            NormalizedUserName = $"{agentId}@test.local".ToUpperInvariant(), IsActive = true
        });
        await db.SaveChangesAsync();

        var lead = await new LeadService(db, adminTenant, new NoOpNotificationService()).CreateAsync(new CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website });
        var project = await new ProjectService(db, adminTenant).CreateAsync(new CreateProjectRequest { Name = "Project " + Guid.NewGuid() });
        var unit = await new UnitService(db, adminTenant, new InMemoryCacheService()).CreateAsync(new CreateUnitRequest
        {
            ProjectId = project.Id, UnitCode = "U-" + Guid.NewGuid().ToString("N")[..6], Price = 2_000_000
        });

        var dealService = new DealService(db, adminTenant, new NoOpNotificationService(), new InMemoryCacheService());
        var deal = await dealService.CreateAsync(new CreateDealRequest
        {
            LeadId = lead.Id, UnitId = unit.Id, SalesAgentId = agentId, DealValue = 2_000_000
        });
        await dealService.ReserveAsync(deal.Id);
        await dealService.ContractAsync(deal.Id);

        return (deal.Id, agentId);
    }

    [Fact]
    public async Task CreateAsync_CalculatesAgentAndCompanyCommission_FromDealValue()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var adminTenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, adminTenant);

        var (dealId, agentId) = await SeedContractedDealAsync(db, companyId, adminTenant);
        var commissionService = new CommissionService(db, adminTenant);

        var commission = await commissionService.CreateAsync(new CreateCommissionRequest
        {
            DealId = dealId,
            CommissionPercentage = 3,
            CompanyCommissionPercentage = 2
        });

        Assert.Equal(agentId, commission.AgentId);
        Assert.Equal(60_000m, commission.CommissionAmount); // 2,000,000 * 3%
        Assert.Equal(40_000m, commission.CompanyCommission); // 2,000,000 * 2%
        Assert.Equal(CommissionStatus.Pending, commission.Status);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenDealNotContracted()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var adminTenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, adminTenant);
        db.Users.Add(new ApplicationUser
        {
            Id = adminTenant.UserId!.Value, CompanyId = companyId, FullName = "Admin", Email = "admin@test.local",
            NormalizedEmail = "ADMIN@TEST.LOCAL", UserName = "admin@test.local", NormalizedUserName = "ADMIN@TEST.LOCAL", IsActive = true
        });
        await db.SaveChangesAsync();

        var lead = await new LeadService(db, adminTenant, new NoOpNotificationService()).CreateAsync(new CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website });
        var project = await new ProjectService(db, adminTenant).CreateAsync(new CreateProjectRequest { Name = "P" });
        var unit = await new UnitService(db, adminTenant, new InMemoryCacheService()).CreateAsync(new CreateUnitRequest { ProjectId = project.Id, UnitCode = "U-1", Price = 1_000_000 });
        var deal = await new DealService(db, adminTenant, new NoOpNotificationService(), new InMemoryCacheService()).CreateAsync(new CreateDealRequest { LeadId = lead.Id, UnitId = unit.Id, DealValue = 1_000_000 });

        var commissionService = new CommissionService(db, adminTenant);

        var ex = await Assert.ThrowsAsync<AppException>(() => commissionService.CreateAsync(new CreateCommissionRequest
        {
            DealId = deal.Id, CommissionPercentage = 3, CompanyCommissionPercentage = 2
        }));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenCommissionAlreadyExistsForDeal()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var adminTenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, adminTenant);

        var (dealId, _) = await SeedContractedDealAsync(db, companyId, adminTenant);
        var commissionService = new CommissionService(db, adminTenant);
        await commissionService.CreateAsync(new CreateCommissionRequest { DealId = dealId, CommissionPercentage = 3, CompanyCommissionPercentage = 2 });

        var ex = await Assert.ThrowsAsync<AppException>(() => commissionService.CreateAsync(new CreateCommissionRequest
        {
            DealId = dealId, CommissionPercentage = 3, CompanyCommissionPercentage = 2
        }));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task SalesAgent_CannotCreateCommission()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var adminTenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, adminTenant);

        var (dealId, agentId) = await SeedContractedDealAsync(db, companyId, adminTenant);
        var agentTenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = agentId, Roles = { Roles.SalesAgent } };
        var commissionService = new CommissionService(db, agentTenant);

        var ex = await Assert.ThrowsAsync<AppException>(() => commissionService.CreateAsync(new CreateCommissionRequest
        {
            DealId = dealId, CommissionPercentage = 3, CompanyCommissionPercentage = 2
        }));

        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task MarkPaidAsync_SetsStatusAndPaymentDate()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var adminTenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, adminTenant);

        var (dealId, _) = await SeedContractedDealAsync(db, companyId, adminTenant);
        var commissionService = new CommissionService(db, adminTenant);
        var commission = await commissionService.CreateAsync(new CreateCommissionRequest { DealId = dealId, CommissionPercentage = 3, CompanyCommissionPercentage = 2 });

        var paid = await commissionService.MarkPaidAsync(commission.Id);

        Assert.Equal(CommissionStatus.Paid, paid.Status);
        Assert.NotNull(paid.PaymentDate);
    }

    [Fact]
    public async Task CancelAsync_Fails_WhenAlreadyPaid()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var adminTenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, adminTenant);

        var (dealId, _) = await SeedContractedDealAsync(db, companyId, adminTenant);
        var commissionService = new CommissionService(db, adminTenant);
        var commission = await commissionService.CreateAsync(new CreateCommissionRequest { DealId = dealId, CommissionPercentage = 3, CompanyCommissionPercentage = 2 });
        await commissionService.MarkPaidAsync(commission.Id);

        var ex = await Assert.ThrowsAsync<AppException>(() => commissionService.CancelAsync(commission.Id));
        Assert.Equal(400, ex.StatusCode);
    }
}
