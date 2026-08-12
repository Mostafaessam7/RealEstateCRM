using RealEstateCRM.Application.Marketing.DTOs;

namespace RealEstateCRM.Application.Marketing;

public interface ICampaignService
{
    Task<IReadOnlyList<CampaignDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<CampaignDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates a Draft campaign. Does not send.</summary>
    Task<CampaignDto> CreateAsync(CreateCampaignRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a Draft campaign to every Lead matching TargetStatus/TargetSource (no filter means
    /// all leads), one delivery attempt per Lead, and transitions it to Sent.
    /// </summary>
    Task<CampaignDto> SendAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CampaignRecipientDto>> ListRecipientsAsync(Guid campaignId, CancellationToken cancellationToken = default);
}
