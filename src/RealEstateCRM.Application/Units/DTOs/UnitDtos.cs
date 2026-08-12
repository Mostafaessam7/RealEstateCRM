using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Application.Units.DTOs;

public class UnitDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string? PropertyType { get; set; }
    public decimal Price { get; set; }
    public decimal? Area { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public string? Floor { get; set; }
    public string? Location { get; set; }
    public UnitStatus Status { get; set; }
    public decimal? DownPayment { get; set; }
    public int? InstallmentYears { get; set; }
    public string? Description { get; set; }
    public bool IsPubliclyListed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Never contains CompanyId — that comes from the authenticated context.</summary>
public class CreateUnitRequest
{
    public Guid ProjectId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string? PropertyType { get; set; }
    public decimal Price { get; set; }
    public decimal? Area { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public string? Floor { get; set; }
    public string? Location { get; set; }
    public UnitStatus Status { get; set; } = UnitStatus.Available;
    public decimal? DownPayment { get; set; }
    public int? InstallmentYears { get; set; }
    public string? Description { get; set; }
    public bool IsPubliclyListed { get; set; }
}

public class UpdateUnitRequest : CreateUnitRequest
{
}

public class UnitListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>Matches UnitCode, Project name, or Location.</summary>
    public string? Search { get; set; }

    public UnitStatus? Status { get; set; }
    public Guid? ProjectId { get; set; }
    public string? PropertyType { get; set; }
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "desc";
}
