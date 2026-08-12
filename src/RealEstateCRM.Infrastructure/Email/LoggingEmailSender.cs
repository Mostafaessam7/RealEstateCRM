using Microsoft.Extensions.Logging;
using RealEstateCRM.Application.Common.Interfaces;

namespace RealEstateCRM.Infrastructure.Email;

/// <summary>
/// Fallback IEmailSender used when Smtp:Host isn't configured — see SmtpEmailSender for the
/// real implementation and DependencyInjection for the config-gated selection between the two.
/// Logs instead of sending so forgot-password/reset-password flows are wired end-to-end without
/// an SMTP account. Never logs the email body, since it carries the reset token.
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email sender not configured. Would send {Subject} to {To}.", subject, to);
        return Task.CompletedTask;
    }
}
