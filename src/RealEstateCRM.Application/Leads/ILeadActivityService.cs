using RealEstateCRM.Application.Leads.DTOs;

namespace RealEstateCRM.Application.Leads;

public interface ILeadActivityService
{
    Task<LeadActivityDto> AddActivityAsync(Guid leadId, CreateLeadActivityRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeadActivityDto>> GetTimelineAsync(Guid leadId, CancellationToken cancellationToken = default);

    /// <summary>FollowUp-type activities scheduled within the next <paramref name="days"/> days.</summary>
    Task<IReadOnlyList<LeadActivityDto>> GetUpcomingFollowUpsAsync(int days, CancellationToken cancellationToken = default);
}
