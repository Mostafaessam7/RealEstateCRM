namespace RealEstateCRM.Infrastructure.WhatsApp;

/// <summary>Bound from configuration ("WhatsApp" section) / environment — never hardcoded.</summary>
public class WhatsAppCloudApiOptions
{
    public const string SectionName = "WhatsApp";

    /// <summary>Meta WhatsApp Business phone number id (the sender).</summary>
    public string? PhoneNumberId { get; set; }

    /// <summary>Permanent or system-user access token for the Graph API.</summary>
    public string? AccessToken { get; set; }

    /// <summary>Graph API version, e.g. "v20.0".</summary>
    public string ApiVersion { get; set; } = "v20.0";
}
