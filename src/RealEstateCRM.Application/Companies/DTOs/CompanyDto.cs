namespace RealEstateCRM.Application.Companies.DTOs;

public class CompanyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}
