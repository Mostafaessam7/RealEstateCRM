using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Domain.Common;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, ITenantScopedDbContext
{
    private readonly ICurrentTenantService _currentTenant;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentTenantService currentTenant,
        IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _currentTenant = currentTenant;
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadActivity> LeadActivities => Set<LeadActivity>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<Commission> Commissions => Set<Commission>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ProjectImage> ProjectImages => Set<ProjectImage>();
    public DbSet<UnitImage> UnitImages => Set<UnitImage>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<CompanySubscription> CompanySubscriptions => Set<CompanySubscription>();
    public DbSet<WhatsAppTemplate> WhatsAppTemplates => Set<WhatsAppTemplate>();
    public DbSet<WhatsAppMessage> WhatsAppMessages => Set<WhatsAppMessage>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignRecipient> CampaignRecipients => Set<CampaignRecipient>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<Payment> Payments => Set<Payment>();

    public Guid? CurrentCompanyId => _currentTenant.CompanyId;

    /// <summary>
    /// Explicit, deliberate cross-tenant access for platform administration. Never use this
    /// to satisfy a normal tenant-scoped request — see docs/multi-tenancy.md#superadmin.
    /// </summary>
    public IQueryable<TEntity> ForAllTenants<TEntity>() where TEntity : class, ITenantEntity =>
        Set<TEntity>().IgnoreQueryFilters();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.ApplyGlobalQueryFilters(this);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.ApplyTenantOnAdd(_currentTenant);
        this.ApplyAuditOnSave(_currentTenant, GetClientIp());
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.ApplyTenantOnAdd(_currentTenant);
        this.ApplyAuditOnSave(_currentTenant, GetClientIp());
        return base.SaveChangesAsync(cancellationToken);
    }

    private string? GetClientIp() => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
