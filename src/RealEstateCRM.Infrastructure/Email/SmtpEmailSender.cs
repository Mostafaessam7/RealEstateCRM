using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RealEstateCRM.Application.Common.Interfaces;

namespace RealEstateCRM.Infrastructure.Email;

/// <summary>
/// Real SMTP delivery via System.Net.Mail. Only registered when Smtp:Host is configured — see
/// DependencyInjection. Never logs the message body (it can carry a password-reset token).
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress ?? _options.Username ?? "no-reply@localhost", _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };
        message.To.Add(to);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            // Never rethrow into the caller's flow (e.g. forgot-password) — a delivery failure
            // must not leak whether an account exists, and must not surface an SMTP stack trace.
            _logger.LogError(ex, "Failed to send email {Subject} to {To}.", subject, to);
        }
    }
}
