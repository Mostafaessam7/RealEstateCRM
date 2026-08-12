using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Leads;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Leads;

public class LeadActivityService : ILeadActivityService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;

    public LeadActivityService(ApplicationDbContext db, ICurrentTenantService currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<LeadActivityDto> AddActivityAsync(Guid leadId, CreateLeadActivityRequest request, CancellationToken cancellationToken = default)
    {
        var leadExists = await _db.Leads.AnyAsync(l => l.Id == leadId, cancellationToken);
        if (!leadExists)
        {
            throw new AppException("Lead not found.", 404);
        }

        var userId = _currentTenant.UserId
            ?? throw new AppException("Authenticated user context is required.", 401);

        var activity = new LeadActivity
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            UserId = userId,
            Type = request.Type,
            Description = request.Description,
            ActivityDate = request.ActivityDate ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.LeadActivities.Add(activity);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(activity);
    }

    public async Task<IReadOnlyList<LeadActivityDto>> GetTimelineAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        var leadExists = await _db.Leads.AnyAsync(l => l.Id == leadId, cancellationToken);
        if (!leadExists)
        {
            throw new AppException("Lead not found.", 404);
        }

        var activities = await _db.LeadActivities
            .AsNoTracking()
            .Where(a => a.LeadId == leadId)
            .OrderByDescending(a => a.ActivityDate)
            .ToListAsync(cancellationToken);

        return activities.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<LeadActivityDto>> GetUpcomingFollowUpsAsync(int days, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(Math.Max(days, 0));

        var activities = await _db.LeadActivities
            .AsNoTracking()
            .Where(a => a.Type == LeadActivityType.FollowUp && a.ActivityDate >= now && a.ActivityDate <= horizon)
            .OrderBy(a => a.ActivityDate)
            .ToListAsync(cancellationToken);

        return activities.Select(ToDto).ToList();
    }

    private static LeadActivityDto ToDto(LeadActivity a) => new()
    {
        Id = a.Id,
        LeadId = a.LeadId,
        UserId = a.UserId,
        Type = a.Type,
        Description = a.Description,
        ActivityDate = a.ActivityDate,
        CreatedAt = a.CreatedAt
    };
}
