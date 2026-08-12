using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Documents.DTOs;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Documents;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Storage;

public class DocumentServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    [Fact]
    public async Task UploadAsync_Fails_WhenLeadDoesNotExist()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new FakeCurrentTenantService { CompanyId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var service = new DocumentService(db, tenant, new InMemoryBlobStorageService());

        using var stream = new MemoryStream(new byte[] { 1, 2 });
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.UploadAsync(new UploadDocumentRequest { LeadId = Guid.NewGuid() }, stream, "id.pdf", "application/pdf", 2));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UploadAsync_LinksToLead_AndListsByLeadId()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = userId };
        await using var db = CreateDb(dbName, tenant);

        var lead = await new LeadService(db, tenant, new NoOpNotificationService())
            .CreateAsync(new CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website });

        var blobStorage = new InMemoryBlobStorageService();
        var service = new DocumentService(db, tenant, blobStorage);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var doc = await service.UploadAsync(new UploadDocumentRequest { LeadId = lead.Id }, stream, "id.pdf", "application/pdf", 3);

        Assert.Equal(userId, doc.UploadedByUserId);
        Assert.Equal(lead.Id, doc.LeadId);

        var listed = await service.ListAsync(leadId: lead.Id, dealId: null);
        Assert.Single(listed);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRowAndDeletesBlob()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new FakeCurrentTenantService { CompanyId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var blobStorage = new InMemoryBlobStorageService();
        var service = new DocumentService(db, tenant, blobStorage);

        using var stream = new MemoryStream(new byte[] { 9 });
        var doc = await service.UploadAsync(new UploadDocumentRequest(), stream, "notes.txt", "text/plain", 1);

        await service.DeleteAsync(doc.Id);

        Assert.Single(blobStorage.DeletedPaths);
        Assert.Empty(await service.ListAsync(null, null));
    }
}
