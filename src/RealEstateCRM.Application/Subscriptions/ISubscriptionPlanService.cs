using RealEstateCRM.Application.Subscriptions.DTOs;

namespace RealEstateCRM.Application.Subscriptions;

public interface ISubscriptionPlanService
{
    Task<IReadOnlyList<SubscriptionPlanDto>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default);

    /// <summary>SuperAdmin only.</summary>
    Task<SubscriptionPlanDto> CreateAsync(CreateSubscriptionPlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>SuperAdmin only.</summary>
    Task<SubscriptionPlanDto> UpdateAsync(Guid id, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken = default);
}
