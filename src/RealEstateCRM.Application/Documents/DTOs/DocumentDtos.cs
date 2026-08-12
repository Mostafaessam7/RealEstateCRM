namespace RealEstateCRM.Application.Documents.DTOs;

public class DocumentDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public Guid UploadedByUserId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? DealId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UploadDocumentRequest
{
    public Guid? LeadId { get; set; }
    public Guid? DealId { get; set; }
}
