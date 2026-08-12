using System.Net;

namespace RealEstateCRM.Tests.Webhooks;

internal class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    public FakeHttpMessageHandler(HttpStatusCode statusCode)
    {
        _statusCode = statusCode;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(_statusCode);
    }
}

internal class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _client;
    public FakeHttpClientFactory(HttpMessageHandler handler) => _client = new HttpClient(handler);
    public HttpClient CreateClient(string name) => _client;
}
