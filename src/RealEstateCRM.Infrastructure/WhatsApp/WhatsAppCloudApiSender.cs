using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RealEstateCRM.Application.Common.Interfaces;

namespace RealEstateCRM.Infrastructure.WhatsApp;

/// <summary>
/// Real delivery via Meta's WhatsApp Business Cloud API (plain HTTPS + Graph API, no vendor SDK
/// needed). Only registered when WhatsApp:PhoneNumberId and WhatsApp:AccessToken are both
/// configured — see DependencyInjection. Uses the standard "text" message type; the recipient
/// must have messaged the business number within the last 24h, or a template message is
/// required instead — a limitation of the WhatsApp Cloud API itself, not this integration.
/// </summary>
public class WhatsAppCloudApiSender : IWhatsAppSender
{
    private readonly WhatsAppCloudApiOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhatsAppCloudApiSender> _logger;

    public WhatsAppCloudApiSender(IOptions<WhatsAppCloudApiOptions> options, HttpClient httpClient, ILogger<WhatsAppCloudApiSender> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string toPhone, string body, CancellationToken cancellationToken = default)
    {
        var url = $"https://graph.facebook.com/{_options.ApiVersion}/{_options.PhoneNumberId}/messages";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        request.Content = JsonContent.Create(new
        {
            messaging_product = "whatsapp",
            to = toPhone,
            type = "text",
            text = new { body },
        });

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("WhatsApp Cloud API rejected a message to {ToPhone}: {StatusCode} {Error}", toPhone, response.StatusCode, error);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reach WhatsApp Cloud API for {ToPhone}.", toPhone);
            return false;
        }
    }
}
