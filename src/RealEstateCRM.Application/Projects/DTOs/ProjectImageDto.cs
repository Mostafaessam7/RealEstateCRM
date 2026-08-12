namespace RealEstateCRM.Application.Projects.DTOs;

public class ProjectImageDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
