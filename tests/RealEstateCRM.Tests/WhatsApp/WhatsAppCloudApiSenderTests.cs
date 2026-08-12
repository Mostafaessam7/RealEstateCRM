using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RealEstateCRM.Infrastructure.WhatsApp;
using RealEstateCRM.Tests.Webhooks;
using Xunit;

namespace RealEstateCRM.Tests.WhatsApp;

public class WhatsAppCloudApiSenderTests
{
    private static WhatsAppCloudApiSender CreateSender(HttpMessageHandler handler) =>
        new(
            Options.Create(new WhatsAppCloudApiOptions { PhoneNumberId = "123456", AccessToken = "test-token" }),
            new HttpClient(handler),
            NullLogger<WhatsAppCloudApiSender>.Instance);

    [Fact]
    public async Task SendAsync_ReturnsTrue_OnSuccessResponse()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var sender = CreateSender(handler);

        var result = await sender.SendAsync("+15551234567", "Hello from tests");

        Assert.True(result);
        Assert.NotNull(handler.LastRequest);
        Assert.Contains("graph.facebook.com", handler.LastRequest!.RequestUri!.Host);
        Assert.Contains("Hello from tests", handler.LastRequestBody);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("test-token", handler.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task SendAsync_ReturnsFalse_OnNonSuccessResponse()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest);
        var sender = CreateSender(handler);

        var result = await sender.SendAsync("+15551234567", "Hello");

        Assert.False(result);
    }

    [Fact]
    public async Task SendAsync_ReturnsFalse_WhenHttpClientThrows()
    {
        var sender = CreateSender(new ThrowingHandler());

        var result = await sender.SendAsync("+15551234567", "Hello");

        Assert.False(result);
    }

    private class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("network down");
    }
}
