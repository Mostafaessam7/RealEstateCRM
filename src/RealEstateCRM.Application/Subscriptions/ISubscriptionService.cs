using RealEstateCRM.Application.Subscriptions.DTOs;

namespace RealEstateCRM.Application.Subscriptions;

public interface ISubscriptionService
{
    /// <summary>Auto-provisions a Free-plan trial subscription on first access for the current company.</summary>
    Task<CompanySubscriptionDto> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>CompanyAdmin/SuperAdmin only. Switches the current company to a different active plan.</summary>
    Task<CompanySubscriptionDto> ChangePlanAsync(ChangePlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>CompanyAdmin/SuperAdmin only.</summary>
    Task<CompanySubscriptionDto> CancelAsync(CancellationToken cancellationToken = default);
}
