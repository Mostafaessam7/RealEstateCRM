using Microsoft.Extensions.Logging;
using RealEstateCRM.Application.Common.Interfaces;

namespace RealEstateCRM.Infrastructure.WhatsApp;

/// <summary>
/// Fallback IWhatsAppSender used when WhatsApp:PhoneNumberId/AccessToken aren't configured —
/// see WhatsAppCloudApiSender for the real Meta WhatsApp Business Cloud API implementation and
/// DependencyInjection for the config-gated selection between the two. Logs instead of sending
/// so the send/history flow is wired end-to-end without a WhatsApp Business account.
/// </summary>
public class LoggingWhatsAppSender : IWhatsAppSender
{
    private readonly ILogger<LoggingWhatsAppSender> _logger;

    public LoggingWhatsAppSender(ILogger<LoggingWhatsAppSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(string toPhone, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WhatsApp sender not configured. Would send message to {ToPhone}.", toPhone);
        return Task.FromResult(true);
    }
}
