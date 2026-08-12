using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Marketing;
using RealEstateCRM.Application.Marketing.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Marketing;

public class CampaignService : ICampaignService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IEmailSender _emailSender;
    private readonly IWhatsAppSender _whatsAppSender;

    public CampaignService(ApplicationDbContext db, ICurrentTenantService currentTenant, IEmailSender emailSender, IWhatsAppSender whatsAppSender)
    {
        _db = db;
        _currentTenant = currentTenant;
        _emailSender = emailSender;
        _whatsAppSender = whatsAppSender;
    }

    public async Task<IReadOnlyList<CampaignDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var campaigns = await _db.Campaigns.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return campaigns.Select(ToDto).ToList();
    }

    public async Task<CampaignDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var campaign = await _db.Campaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new AppException("Campaign not found.", 404);

        return ToDto(campaign);
    }

    public async Task<CampaignDto> CreateAsync(CreateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        EnsureElevatedAccess();

        var userId = _currentTenant.UserId ?? throw new AppException("Authenticated user context is required.", 401);

        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Channel = request.Channel,
            Subject = request.Subject,
            Body = request.Body,
            TargetStatus = request.TargetStatus,
            TargetSource = request.TargetSource,
            Status = CampaignStatus.Draft,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(campaign);
    }

    public async Task<CampaignDto> SendAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureElevatedAccess();

        var campaign = await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new AppException("Campaign not found.", 404);

        if (campaign.Status != CampaignStatus.Draft)
        {
            throw new AppException("Only a Draft campaign can be sent.", 400);
        }

        var leads = _db.Leads.AsNoTracking().AsQueryable();
        if (campaign.TargetStatus.HasValue)
        {
            leads = leads.Where(l => l.Status == campaign.TargetStatus.Value);
        }
        if (campaign.TargetSource.HasValue)
        {
            leads = leads.Where(l => l.Source == campaign.TargetSource.Value);
        }
        if (campaign.Channel == CampaignChannel.WhatsApp)
        {
            leads = leads.Where(l => l.Phone != null);
        }
        else
        {
            leads = leads.Where(l => l.Email != null);
        }

        var recipients = await leads.ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var successCount = 0;
        var failureCount = 0;

        foreach (var lead in recipients)
        {
            var body = RenderTemplate(campaign.Body, lead);
            var success = false;
            string? errorMessage = null;

            try
            {
                if (campaign.Channel == CampaignChannel.WhatsApp)
                {
                    success = await _whatsAppSender.SendAsync(lead.Phone!, body, cancellationToken);
                    if (!success) errorMessage = "The WhatsApp provider rejected the message.";
                }
                else
                {
                    await _emailSender.SendAsync(lead.Email!, campaign.Subject ?? campaign.Name, body, cancellationToken);
                    success = true;
                }
            }
            catch (Exception ex)
            {
                success = false;
                errorMessage = ex.Message;
            }

            _db.CampaignRecipients.Add(new CampaignRecipient
            {
                Id = Guid.NewGuid(),
                CampaignId = campaign.Id,
                LeadId = lead.Id,
                Success = success,
                ErrorMessage = errorMessage,
                SentAt = now
            });

            if (success) successCount++; else failureCount++;
        }

        campaign.Status = CampaignStatus.Sent;
        campaign.SentAt = now;
        campaign.RecipientCount = recipients.Count;
        campaign.SuccessCount = successCount;
        campaign.FailureCount = failureCount;
        campaign.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(campaign);
    }

    public async Task<IReadOnlyList<CampaignRecipientDto>> ListRecipientsAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var recipients = await _db.CampaignRecipients.AsNoTracking()
            .Where(r => r.CampaignId == campaignId)
            .OrderByDescending(r => r.SentAt)
            .ToListAsync(cancellationToken);

        return recipients.Select(r => new CampaignRecipientDto
        {
            Id = r.Id,
            LeadId = r.LeadId,
            Success = r.Success,
            ErrorMessage = r.ErrorMessage,
            SentAt = r.SentAt
        }).ToList();
    }

    private static string RenderTemplate(string body, Lead lead) => body
        .Replace("{{FullName}}", lead.FullName)
        .Replace("{{PreferredLocation}}", lead.PreferredLocation ?? string.Empty)
        .Replace("{{PropertyType}}", lead.PropertyType ?? string.Empty);

    private void EnsureElevatedAccess()
    {
        var allowed = _currentTenant.IsSuperAdmin ||
            _currentTenant.IsInRole(Roles.CompanyAdmin) ||
            _currentTenant.IsInRole(Roles.SalesManager);

        if (!allowed)
        {
            throw new AppException("You are not authorized to manage marketing campaigns.", 403);
        }
    }

    private static CampaignDto ToDto(Campaign campaign) => new()
    {
        Id = campaign.Id,
        Name = campaign.Name,
        Channel = campaign.Channel,
        Subject = campaign.Subject,
        Body = campaign.Body,
        TargetStatus = campaign.TargetStatus,
        TargetSource = campaign.TargetSource,
        Status = campaign.Status,
        SentAt = campaign.SentAt,
        RecipientCount = campaign.RecipientCount,
        SuccessCount = campaign.SuccessCount,
        FailureCount = campaign.FailureCount,
        CreatedAt = campaign.CreatedAt
    };
}
