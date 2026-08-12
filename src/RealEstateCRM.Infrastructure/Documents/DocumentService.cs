using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Common.Storage;
using RealEstateCRM.Application.Documents;
using RealEstateCRM.Application.Documents.DTOs;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Documents;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IBlobStorageService _blobStorage;

    public DocumentService(ApplicationDbContext db, ICurrentTenantService currentTenant, IBlobStorageService blobStorage)
    {
        _db = db;
        _currentTenant = currentTenant;
        _blobStorage = blobStorage;
    }

    public async Task<DocumentDto> UploadAsync(
        UploadDocumentRequest request, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        UploadValidation.EnsureDocument(sizeBytes);

        if (request.LeadId.HasValue)
        {
            var leadExists = await _db.Leads.AnyAsync(l => l.Id == request.LeadId.Value, cancellationToken);
            if (!leadExists)
            {
                throw new AppException("Lead not found.", 400);
            }
        }

        if (request.DealId.HasValue)
        {
            var dealExists = await _db.Deals.AnyAsync(d => d.Id == request.DealId.Value, cancellationToken);
            if (!dealExists)
            {
                throw new AppException("Deal not found.", 400);
            }
        }

        var companyId = _currentTenant.CompanyId
            ?? throw new AppException("Authenticated company context is required.", 401);
        var userId = _currentTenant.UserId
            ?? throw new AppException("Authenticated user context is required.", 401);

        var documentId = Guid.NewGuid();
        var path = BlobPaths.Document(companyId, documentId, fileName);
        var url = await _blobStorage.UploadAsync(path, content, contentType, cancellationToken);

        var document = new Document
        {
            Id = documentId,
            BlobPath = path,
            Url = url,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            UploadedByUserId = userId,
            LeadId = request.LeadId,
            DealId = request.DealId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Documents.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(document);
    }

    public async Task<IReadOnlyList<DocumentDto>> ListAsync(Guid? leadId, Guid? dealId, CancellationToken cancellationToken = default)
    {
        var documents = _db.Documents.AsNoTracking().AsQueryable();

        if (leadId.HasValue)
        {
            documents = documents.Where(d => d.LeadId == leadId.Value);
        }

        if (dealId.HasValue)
        {
            documents = documents.Where(d => d.DealId == dealId.Value);
        }

        var results = await documents.OrderByDescending(d => d.CreatedAt).ToListAsync(cancellationToken);
        return results.Select(ToDto).ToList();
    }

    public async Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken)
            ?? throw new AppException("Document not found.", 404);

        await _blobStorage.DeleteAsync(document.BlobPath, cancellationToken);

        _db.Documents.Remove(document);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static DocumentDto ToDto(Document document) => new()
    {
        Id = document.Id,
        Url = document.Url,
        FileName = document.FileName,
        ContentType = document.ContentType,
        SizeBytes = document.SizeBytes,
        UploadedByUserId = document.UploadedByUserId,
        LeadId = document.LeadId,
        DealId = document.DealId,
        CreatedAt = document.CreatedAt
    };
}
