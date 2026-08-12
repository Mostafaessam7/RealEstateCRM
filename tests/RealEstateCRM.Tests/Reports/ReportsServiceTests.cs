using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Commissions.DTOs;
using RealEstateCRM.Application.Deals.DTOs;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Application.Projects.DTOs;
using RealEstateCRM.Application.Units.DTOs;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Commissions;
using RealEstateCRM.Infrastructure.Deals;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Projects;
using RealEstateCRM.Infrastructure.Reports;
using RealEstateCRM.Infrastructure.Units;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Reports;

public class ReportsServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    [Fact]
    public async Task GetLeadsReportAsync_GroupsByStatusAndSource()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new FakeCurrentTenantService { CompanyId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var leadService = new LeadService(db, tenant, new NoOpNotificationService());

        await leadService.CreateAsync(new CreateLeadRequest { FullName = "A", Source = LeadSource.Website });
        await leadService.CreateAsync(new CreateLeadRequest { FullName = "B", Source = LeadSource.Facebook });

        var report = await new ReportsService(db).GetLeadsReportAsync();

        Assert.Equal(2, report.TotalLeads);
        Assert.Equal(2, report.ByStatus[LeadStatus.New.ToString()]);
        Assert.Equal(1, report.BySource[LeadSource.Website.ToString()]);
        Assert.Equal(1, report.BySource[LeadSource.Facebook.ToString()]);
    }

    [Fact]
    public async Task GetInventoryReportAsync_CountsUnitsByStatus()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new FakeCurrentTenantService { CompanyId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);

        var project = await new ProjectService(db, tenant).CreateAsync(new CreateProjectRequest { Name = "P" });
        var unitService = new UnitService(db, tenant, new InMemoryCacheService());
        await unitService.CreateAsync(new CreateUnitRequest { ProjectId = project.Id, UnitCode = "A-1", Price = 1, Status = UnitStatus.Available });
        await unitService.CreateAsync(new CreateUnitRequest { ProjectId = project.Id, UnitCode = "A-2", Price = 1, Status = UnitStatus.Sold });

        var report = await new ReportsService(db).GetInventoryReportAsync();

        Assert.Equal(1, report.TotalProjects);
        Assert.Equal(2, report.TotalUnits);
        Assert.Equal(1, report.UnitsByStatus[UnitStatus.Available.ToString()]);
        Assert.Equal(1, report.UnitsByStatus[UnitStatus.Sold.ToString()]);
    }

    [Fact]
    public async Task GetConversionReportAsync_ComputesPercentCorrectly()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new FakeCurrentTenantService { CompanyId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var leadService = new LeadService(db, tenant, new NoOpNotificationService());

        var lead1 = await leadService.CreateAsync(new CreateLeadRequest { FullName = "A", Source = LeadSource.Website });
        await leadService.CreateAsync(new CreateLeadRequest { FullName = "B", Source = LeadSource.Website });
        await leadService.UpdateAsync(lead1.Id, new UpdateLeadRequest { FullName = "A", Source = LeadSource.Website, Status = LeadStatus.Contracted });

        var report = await new ReportsService(db).GetConversionReportAsync();

        Assert.Equal(2, report.TotalLeads);
        Assert.Equal(1, report.ConvertedLeads);
        Assert.Equal(50.0, report.ConversionRatePercent);
    }

    [Fact]
    public async Task GetCommissionReportAsync_SumsAmountsByStatus()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var adminTenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Domain.Constants.Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, adminTenant);
        db.Users.Add(new ApplicationUser
        {
            Id = adminTenant.UserId!.Value, CompanyId = companyId, FullName = "Admin", Email = "admin@test.local",
            NormalizedEmail = "ADMIN@TEST.LOCAL", UserName = "admin@test.local", NormalizedUserName = "ADMIN@TEST.LOCAL", IsActive = true
        });
        await db.SaveChangesAsync();

        var lead = await new LeadService(db, adminTenant, new NoOpNotificationService())
            .CreateAsync(new CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website });
        var project = await new ProjectService(db, adminTenant).CreateAsync(new CreateProjectRequest { Name = "P" });
        var unit = await new UnitService(db, adminTenant, new InMemoryCacheService())
            .CreateAsync(new CreateUnitRequest { ProjectId = project.Id, UnitCode = "U-1", Price = 1_000_000 });

        var dealService = new DealService(db, adminTenant, new NoOpNotificationService(), new InMemoryCacheService());
        var deal = await dealService.CreateAsync(new CreateDealRequest { LeadId = lead.Id, UnitId = unit.Id, DealValue = 1_000_000 });
        await dealService.ReserveAsync(deal.Id);
        await dealService.ContractAsync(deal.Id);

        var commissionService = new CommissionService(db, adminTenant);
        var commission = await commissionService.CreateAsync(new CreateCommissionRequest
        {
            DealId = deal.Id, CommissionPercentage = 3, CompanyCommissionPercentage = 2
        });
        await commissionService.MarkPaidAsync(commission.Id);

        var report = await new ReportsService(db).GetCommissionReportAsync();

        Assert.Equal(30_000m, report.TotalPaid);
        Assert.Equal(0m, report.TotalPending);
    }
}
