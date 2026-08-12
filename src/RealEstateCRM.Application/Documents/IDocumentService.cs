using RealEstateCRM.Application.Documents.DTOs;

namespace RealEstateCRM.Application.Documents;

public interface IDocumentService
{
    Task<DocumentDto> UploadAsync(
        UploadDocumentRequest request, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentDto>> ListAsync(Guid? leadId, Guid? dealId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default);
}
