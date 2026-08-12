using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealEstateCRM.Application.AiAssistant;
using RealEstateCRM.Application.ApiKeys;
using RealEstateCRM.Application.Auditing;
using RealEstateCRM.Application.Auth;
using RealEstateCRM.Application.Commissions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Companies;
using RealEstateCRM.Application.Dashboard;
using RealEstateCRM.Application.Deals;
using RealEstateCRM.Application.Documents;
using RealEstateCRM.Application.Leads;
using RealEstateCRM.Application.Marketing;
using RealEstateCRM.Application.Marketplace;
using RealEstateCRM.Application.Payments;
using RealEstateCRM.Application.Notifications;
using RealEstateCRM.Application.Projects;
using RealEstateCRM.Application.Recommendations;
using RealEstateCRM.Application.Reports;
using RealEstateCRM.Application.Subscriptions;
using RealEstateCRM.Application.Tasks;
using RealEstateCRM.Application.Units;
using RealEstateCRM.Application.Users;
using RealEstateCRM.Application.Webhooks;
using RealEstateCRM.Application.WhatsApp;
using RealEstateCRM.Infrastructure.AiAssistant;
using RealEstateCRM.Infrastructure.ApiKeys;
using RealEstateCRM.Infrastructure.Auditing;
using RealEstateCRM.Infrastructure.Auth;
using RealEstateCRM.Infrastructure.Caching;
using RealEstateCRM.Infrastructure.Commissions;
using RealEstateCRM.Infrastructure.Companies;
using RealEstateCRM.Infrastructure.Dashboard;
using RealEstateCRM.Infrastructure.Deals;
using RealEstateCRM.Infrastructure.Documents;
using RealEstateCRM.Infrastructure.Email;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Jobs;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Marketing;
using RealEstateCRM.Infrastructure.Marketplace;
using RealEstateCRM.Infrastructure.Payments;
using RealEstateCRM.Infrastructure.Notifications;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Projects;
using RealEstateCRM.Infrastructure.Recommendations;
using RealEstateCRM.Infrastructure.Reports;
using RealEstateCRM.Infrastructure.Storage;
using RealEstateCRM.Infrastructure.Subscriptions;
using RealEstateCRM.Infrastructure.Tasks;
using RealEstateCRM.Infrastructure.Units;
using RealEstateCRM.Infrastructure.Users;
using RealEstateCRM.Infrastructure.Webhooks;
using RealEstateCRM.Infrastructure.WhatsApp;

namespace RealEstateCRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();

        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        var smtpHost = configuration[$"{SmtpOptions.SectionName}:Host"];
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<ILeadActivityService, LeadActivityService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<IDealService, DealService>();
        services.AddScoped<ICommissionService, CommissionService>();
        services.AddScoped<ITaskItemService, TaskItemService>();
        services.AddScoped<ITenantScope, TenantScope>();
        services.AddScoped<ReminderJobs>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        services.AddScoped<IProjectImageService, ProjectImageService>();
        services.AddScoped<IUnitImageService, UnitImageService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IUserAvatarService, UserAvatarService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IReportsService, ReportsService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<ISubscriptionLimitService, SubscriptionLimitService>();
        services.Configure<WhatsAppCloudApiOptions>(configuration.GetSection(WhatsAppCloudApiOptions.SectionName));
        var whatsAppPhoneNumberId = configuration[$"{WhatsAppCloudApiOptions.SectionName}:PhoneNumberId"];
        var whatsAppAccessToken = configuration[$"{WhatsAppCloudApiOptions.SectionName}:AccessToken"];
        if (string.IsNullOrWhiteSpace(whatsAppPhoneNumberId) || string.IsNullOrWhiteSpace(whatsAppAccessToken))
        {
            services.AddScoped<IWhatsAppSender, LoggingWhatsAppSender>();
        }
        else
        {
            services.AddHttpClient<IWhatsAppSender, WhatsAppCloudApiSender>();
        }
        services.AddScoped<IWhatsAppService, WhatsAppService>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<IAiLeadAssistantService, TemplateAiLeadAssistantService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IMarketplaceService, MarketplaceService>();

        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
        var stripeSecretKey = configuration[$"{StripeOptions.SectionName}:SecretKey"];
        if (string.IsNullOrWhiteSpace(stripeSecretKey))
        {
            services.AddScoped<IPaymentGateway, NoOpPaymentGateway>();
        }
        else
        {
            services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        }
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<IWebhookPublisher, WebhookPublisher>();
        services.AddScoped<WebhookDeliveryJob>();
        services.AddHttpClient("webhooks", client => client.Timeout = TimeSpan.FromSeconds(10));
        services.AddSignalR();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "RealEstateCRM:";
        });
        services.AddScoped<ICacheService, DistributedCacheService>();

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection")));
        services.AddHangfireServer();

        return services;
    }
}
