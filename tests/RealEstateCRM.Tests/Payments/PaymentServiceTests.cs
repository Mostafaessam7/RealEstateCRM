using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Deals.DTOs;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Application.Payments.DTOs;
using RealEstateCRM.Application.Projects.DTOs;
using RealEstateCRM.Application.Units.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Deals;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Payments;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Projects;
using RealEstateCRM.Infrastructure.Units;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Payments;

public class PaymentServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    private static async Task<Guid> SeedDealAsync(ApplicationDbContext db, FakeCurrentTenantService tenant, decimal? downPayment = 100_000)
    {
        db.Users.Add(new ApplicationUser
        {
            Id = tenant.UserId!.Value, CompanyId = tenant.CompanyId!.Value, FullName = "Agent", Email = $"{tenant.UserId}@test.local",
            NormalizedEmail = $"{tenant.UserId}@test.local".ToUpperInvariant(), UserName = $"{tenant.UserId}@test.local",
            NormalizedUserName = $"{tenant.UserId}@test.local".ToUpperInvariant(), IsActive = true
        });
        await db.SaveChangesAsync();

        var lead = await new LeadService(db, tenant, new NoOpNotificationService())
            .CreateAsync(new CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website });
        var project = await new ProjectService(db, tenant).CreateAsync(new CreateProjectRequest { Name = "P" });
        var unit = await new UnitService(db, tenant, new InMemoryCacheService())
            .CreateAsync(new CreateUnitRequest { ProjectId = project.Id, UnitCode = "U-1", Price = 1_000_000, DownPayment = downPayment });
        var deal = await new DealService(db, tenant, new NoOpNotificationService(), new InMemoryCacheService())
            .CreateAsync(new CreateDealRequest { LeadId = lead.Id, UnitId = unit.Id, DealValue = 1_000_000 });

        return deal.Id;
    }

    [Fact]
    public async Task CreateCheckoutAsync_DefaultsAmount_ToUnitDownPayment()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);

        var dealId = await SeedDealAsync(db, tenant, downPayment: 150_000);
        var gateway = new FakePaymentGateway();
        var service = new PaymentService(db, tenant, gateway);

        var checkout = await service.CreateCheckoutAsync(dealId, new CreateCheckoutRequest(), "https://app/success", "https://app/cancel");

        Assert.False(string.IsNullOrWhiteSpace(checkout.CheckoutUrl));

        var payments = await service.ListForDealAsync(dealId);
        Assert.Single(payments);
        Assert.Equal(150_000, payments[0].Amount);
        Assert.Equal(PaymentStatus.Pending, payments[0].Status);
    }

    [Fact]
    public async Task CreateCheckoutAsync_Fails_WhenNoAmountAndNoDownPayment()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);

        var dealId = await SeedDealAsync(db, tenant, downPayment: null);
        var service = new PaymentService(db, tenant, new FakePaymentGateway());

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateCheckoutAsync(dealId, new CreateCheckoutRequest(), "https://app/success", "https://app/cancel"));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task HandleWebhookAsync_MarksPaymentPaid_OnSuccess()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);

        var dealId = await SeedDealAsync(db, tenant);
        var gateway = new FakePaymentGateway { WebhookSucceeded = true };
        var service = new PaymentService(db, tenant, gateway);

        await service.CreateCheckoutAsync(dealId, new CreateCheckoutRequest(), "https://app/success", "https://app/cancel");
        await service.HandleWebhookAsync("{}", "sig");

        var payments = await service.ListForDealAsync(dealId);
        Assert.Equal(PaymentStatus.Paid, payments[0].Status);
        Assert.NotNull(payments[0].PaidAt);
    }

    [Fact]
    public async Task HandleWebhookAsync_MarksPaymentFailed_OnFailure()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);

        var dealId = await SeedDealAsync(db, tenant);
        var gateway = new FakePaymentGateway { WebhookSucceeded = false };
        var service = new PaymentService(db, tenant, gateway);

        await service.CreateCheckoutAsync(dealId, new CreateCheckoutRequest(), "https://app/success", "https://app/cancel");
        await service.HandleWebhookAsync("{}", "sig");

        var payments = await service.ListForDealAsync(dealId);
        Assert.Equal(PaymentStatus.Failed, payments[0].Status);
    }
}
