using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RealEstateCRM.Infrastructure.Email;
using Xunit;

namespace RealEstateCRM.Tests.Email;

public class SmtpEmailSenderTests
{
    [Fact]
    public async Task SendAsync_NeverThrows_WhenHostIsUnreachable()
    {
        // A password-reset flow must never surface an SMTP failure to the caller — that would
        // both break the endpoint and risk leaking account-existence info via error timing.
        var sender = new SmtpEmailSender(
            Options.Create(new SmtpOptions { Host = "smtp.invalid.example", Port = 25, FromAddress = "no-reply@example.com" }),
            NullLogger<SmtpEmailSender>.Instance);

        var exception = await Record.ExceptionAsync(() => sender.SendAsync("user@example.com", "Reset your password", "token123"));

        Assert.Null(exception);
    }
}
