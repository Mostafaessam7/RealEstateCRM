using RealEstateCRM.Domain.Common;

namespace RealEstateCRM.Domain.Entities;

/// <summary>
/// Body supports {{FullName}}, {{PreferredLocation}}, {{PropertyType}} placeholders,
/// substituted from the target Lead when a message is sent.
/// </summary>
public class WhatsAppTemplate : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
