namespace RealEstateCRM.Application.Marketplace.DTOs;

/// <summary>
/// Deliberately minimal — no CompanyId, no internal ids beyond the unit's own, no financial
/// terms beyond price. This is the only DTO in the app served to unauthenticated callers.
/// </summary>
public class PublicUnitDto
{
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string? PropertyType { get; set; }
    public decimal Price { get; set; }
    public decimal? Area { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}

public class PublicUnitListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? PropertyType { get; set; }
    public string? Location { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}
