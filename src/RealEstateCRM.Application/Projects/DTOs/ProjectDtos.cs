using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Application.Projects.DTOs;

public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Developer { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public decimal? StartingPrice { get; set; }
    public ProjectStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Never contains CompanyId — that comes from the authenticated context.</summary>
public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Developer { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public decimal? StartingPrice { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
}

public class UpdateProjectRequest : CreateProjectRequest
{
}

public class ProjectListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public ProjectStatus? Status { get; set; }
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "desc";
}
