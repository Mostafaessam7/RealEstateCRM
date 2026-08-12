using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Auditing;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Auditing;

public class AuditLoggingTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    [Fact]
    public async Task CreatingALead_WritesAnAuditLogEntry()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = userId };
        await using var db = CreateDb(dbName, tenant);
        var leadService = new LeadService(db, tenant, new NoOpNotificationService());

        var lead = await leadService.CreateAsync(new CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website });

        var auditLog = await new AuditLogService(db).ListAsync(new() { EntityName = nameof(Lead), EntityId = lead.Id });

        var entry = Assert.Single(auditLog.Items);
        Assert.Equal("Created", entry.Action);
        Assert.Equal(userId, entry.UserId);
        Assert.Contains("Buyer", entry.NewValues);
    }

    [Fact]
    public async Task UpdatingALead_WritesAnAuditLogEntry_WithOldAndNewValues()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var leadService = new LeadService(db, tenant, new NoOpNotificationService());

        var lead = await leadService.CreateAsync(new CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website });
        await leadService.UpdateAsync(lead.Id, new UpdateLeadRequest
        {
            FullName = "Buyer Updated", Source = LeadSource.Website, Status = LeadStatus.Contacted
        });

        var auditLog = await new AuditLogService(db).ListAsync(new() { EntityName = nameof(Lead), EntityId = lead.Id });

        Assert.Equal(2, auditLog.TotalCount); // Created + Updated
        var updateEntry = auditLog.Items.First(e => e.Action == "Updated");
        Assert.Contains("Buyer Updated", updateEntry.NewValues);
        Assert.Contains("\"Status\":0", updateEntry.OldValues); // LeadStatus.New == 0, the original status
    }

    [Fact]
    public async Task CreatingAUser_NeverIncludesPasswordHashInTheAuditSnapshot()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);

        var userId = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            CompanyId = companyId,
            FullName = "Agent",
            Email = "agent@test.local",
            NormalizedEmail = "AGENT@TEST.LOCAL",
            UserName = "agent@test.local",
            NormalizedUserName = "AGENT@TEST.LOCAL",
            IsActive = true,
            PasswordHash = "super-secret-hash-should-never-appear-in-audit-log",
            SecurityStamp = "some-security-stamp-value"
        });
        await db.SaveChangesAsync();

        var auditLog = await new AuditLogService(db).ListAsync(new() { EntityName = nameof(ApplicationUser), EntityId = userId });

        var entry = Assert.Single(auditLog.Items);
        Assert.DoesNotContain("super-secret-hash-should-never-appear-in-audit-log", entry.NewValues);
        Assert.DoesNotContain("PasswordHash", entry.NewValues);
        Assert.DoesNotContain("some-security-stamp-value", entry.NewValues);
        Assert.DoesNotContain("SecurityStamp", entry.NewValues);
    }

    [Fact]
    public async Task CompanyA_CannotSeeCompanyBAuditLogs()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyAId = Guid.NewGuid();
        var companyBId = Guid.NewGuid();

        var tenantB = new FakeCurrentTenantService { CompanyId = companyBId, UserId = Guid.NewGuid() };
        await using (var dbB = CreateDb(dbName, tenantB))
        {
            await new LeadService(dbB, tenantB, new NoOpNotificationService())
                .CreateAsync(new CreateLeadRequest { FullName = "Company B Lead", Source = LeadSource.Website });
        }

        var tenantA = new FakeCurrentTenantService { CompanyId = companyAId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var dbA = CreateDb(dbName, tenantA);

        var result = await new AuditLogService(dbA).ListAsync(new());

        Assert.Empty(result.Items);
    }
}
