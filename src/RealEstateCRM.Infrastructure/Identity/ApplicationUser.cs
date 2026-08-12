using Microsoft.AspNetCore.Identity;

namespace RealEstateCRM.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Null for platform-level SuperAdmin users.</summary>
    public Guid? CompanyId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? ManagerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string? AvatarBlobPath { get; set; }
    public string? AvatarUrl { get; set; }
}
