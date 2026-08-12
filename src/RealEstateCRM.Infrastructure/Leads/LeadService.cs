using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Leads;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Application.Notifications;
using RealEstateCRM.Application.Subscriptions;
using RealEstateCRM.Application.Webhooks;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Subscriptions;
using RealEstateCRM.Infrastructure.Webhooks;

namespace RealEstateCRM.Infrastructure.Leads;

public class LeadService : ILeadService
{
    private static readonly HashSet<string> SortableFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreatedAt", "FullName", "Status", "BudgetMin", "BudgetMax"
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;
    private readonly INotificationService _notificationService;
    private readonly ISubscriptionLimitService _subscriptionLimit;
    private readonly IWebhookPublisher _webhookPublisher;

    public LeadService(
        ApplicationDbContext db,
        ICurrentTenantService currentTenant,
        INotificationService notificationService,
        ISubscriptionLimitService? subscriptionLimit = null,
        IWebhookPublisher? webhookPublisher = null)
    {
        _db = db;
        _currentTenant = currentTenant;
        _notificationService = notificationService;
        _webhookPublisher = webhookPublisher ?? NullWebhookPublisher.Instance;
        _subscriptionLimit = subscriptionLimit ?? NullSubscriptionLimitService.Instance;
    }

    public async Task<PagedResult<LeadDto>> ListAsync(LeadListQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var leads = _db.Leads.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            leads = leads.Where(l =>
                EF.Functions.Like(l.FullName, $"%{term}%") ||
                (l.Phone != null && EF.Functions.Like(l.Phone, $"%{term}%")) ||
                (l.Email != null && EF.Functions.Like(l.Email, $"%{term}%")));
        }

        if (query.Status.HasValue)
        {
            leads = leads.Where(l => l.Status == query.Status.Value);
        }

        if (query.AssignedAgentId.HasValue)
        {
            leads = leads.Where(l => l.AssignedAgentId == query.AssignedAgentId.Value);
        }

        if (query.Source.HasValue)
        {
            leads = leads.Where(l => l.Source == query.Source.Value);
        }

        var sortBy = SortableFields.Contains(query.SortBy ?? string.Empty) ? query.SortBy! : "CreatedAt";
        var descending = !string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        leads = (sortBy, descending) switch
        {
            ("FullName", true) => leads.OrderByDescending(l => l.FullName),
            ("FullName", false) => leads.OrderBy(l => l.FullName),
            ("Status", true) => leads.OrderByDescending(l => l.Status),
            ("Status", false) => leads.OrderBy(l => l.Status),
            ("BudgetMin", true) => leads.OrderByDescending(l => l.BudgetMin),
            ("BudgetMin", false) => leads.OrderBy(l => l.BudgetMin),
            ("BudgetMax", true) => leads.OrderByDescending(l => l.BudgetMax),
            ("BudgetMax", false) => leads.OrderBy(l => l.BudgetMax),
            (_, true) => leads.OrderByDescending(l => l.CreatedAt),
            _ => leads.OrderBy(l => l.CreatedAt)
        };

        var totalCount = await leads.CountAsync(cancellationToken);
        var items = await leads
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LeadDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<LeadDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lead = await _db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new AppException("Lead not found.", 404);

        return ToDto(lead);
    }

    public async Task<LeadDto> CreateAsync(CreateLeadRequest request, CancellationToken cancellationToken = default)
    {
        await _subscriptionLimit.EnsureCanAddLeadAsync(cancellationToken);
        await EnsureAgentValidAsync(request.AssignedAgentId, cancellationToken);

        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Phone = request.Phone,
            Email = request.Email,
            Source = request.Source,
            Status = LeadStatus.New,
            BudgetMin = request.BudgetMin,
            BudgetMax = request.BudgetMax,
            PreferredLocation = request.PreferredLocation,
            PropertyType = request.PropertyType,
            AssignedAgentId = request.AssignedAgentId,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(cancellationToken);

        await _webhookPublisher.PublishAsync(WebhookEventTypes.LeadCreated, ToDto(lead), cancellationToken);

        return ToDto(lead);
    }

    public async Task<LeadDto> UpdateAsync(Guid id, UpdateLeadRequest request, CancellationToken cancellationToken = default)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new AppException("Lead not found.", 404);

        await EnsureAgentValidAsync(request.AssignedAgentId, cancellationToken);

        var statusChanged = lead.Status != request.Status;

        lead.FullName = request.FullName.Trim();
        lead.Phone = request.Phone;
        lead.Email = request.Email;
        lead.Source = request.Source;
        lead.Status = request.Status;
        lead.BudgetMin = request.BudgetMin;
        lead.BudgetMax = request.BudgetMax;
        lead.PreferredLocation = request.PreferredLocation;
        lead.PropertyType = request.PropertyType;
        lead.AssignedAgentId = request.AssignedAgentId;
        lead.Notes = request.Notes;
        lead.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        if (statusChanged)
        {
            await _webhookPublisher.PublishAsync(WebhookEventTypes.LeadStatusChanged, ToDto(lead), cancellationToken);
        }

        return ToDto(lead);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new AppException("Lead not found.", 404);

        lead.IsDeleted = true;
        lead.DeletedAt = DateTime.UtcNow;
        lead.DeletedBy = _currentTenant.UserId;
        lead.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<LeadDto> AssignAsync(Guid id, AssignLeadRequest request, CancellationToken cancellationToken = default)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new AppException("Lead not found.", 404);

        if (lead.AssignedAgentId.HasValue)
        {
            throw new AppException("Lead is already assigned. Use transfer instead.", 409);
        }

        await EnsureAgentValidAsync(request.AgentId, cancellationToken);

        lead.AssignedAgentId = request.AgentId;
        lead.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyUserAsync(
            request.AgentId,
            "LeadAssigned",
            "New lead assigned",
            $"You've been assigned to {lead.FullName}.",
            cancellationToken);

        return ToDto(lead);
    }

    public async Task<LeadDto> TransferAsync(Guid id, AssignLeadRequest request, CancellationToken cancellationToken = default)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new AppException("Lead not found.", 404);

        if (!lead.AssignedAgentId.HasValue)
        {
            throw new AppException("Lead has no current agent to transfer from. Use assign instead.", 400);
        }

        if (lead.AssignedAgentId == request.AgentId)
        {
            throw new AppException("Lead is already assigned to this agent.", 400);
        }

        await EnsureAgentValidAsync(request.AgentId, cancellationToken);

        var previousAgentId = lead.AssignedAgentId;
        lead.AssignedAgentId = request.AgentId;
        lead.UpdatedAt = DateTime.UtcNow;

        if (_currentTenant.UserId.HasValue)
        {
            _db.LeadActivities.Add(new LeadActivity
            {
                Id = Guid.NewGuid(),
                CompanyId = lead.CompanyId,
                LeadId = lead.Id,
                UserId = _currentTenant.UserId.Value,
                Type = LeadActivityType.Note,
                Description = $"Lead transferred from agent {previousAgentId} to agent {request.AgentId}.",
                ActivityDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyUserAsync(
            request.AgentId,
            "LeadAssigned",
            "Lead transferred to you",
            $"You've been assigned to {lead.FullName}.",
            cancellationToken);

        return ToDto(lead);
    }

    private async Task EnsureAgentValidAsync(Guid? agentId, CancellationToken cancellationToken)
    {
        if (!agentId.HasValue)
        {
            return;
        }

        var exists = await _db.Users.AnyAsync(
            u => u.Id == agentId.Value && u.CompanyId == _currentTenant.CompanyId,
            cancellationToken);

        if (!exists)
        {
            throw new AppException("Assigned agent not found in this company.", 400);
        }
    }

    private static LeadDto ToDto(Lead lead) => new()
    {
        Id = lead.Id,
        FullName = lead.FullName,
        Phone = lead.Phone,
        Email = lead.Email,
        Source = lead.Source,
        Status = lead.Status,
        BudgetMin = lead.BudgetMin,
        BudgetMax = lead.BudgetMax,
        PreferredLocation = lead.PreferredLocation,
        PropertyType = lead.PropertyType,
        AssignedAgentId = lead.AssignedAgentId,
        Notes = lead.Notes,
        CreatedAt = lead.CreatedAt,
        UpdatedAt = lead.UpdatedAt
    };
}
