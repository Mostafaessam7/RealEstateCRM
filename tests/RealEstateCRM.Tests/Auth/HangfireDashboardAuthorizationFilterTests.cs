using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using RealEstateCRM.Infrastructure.Auth;
using Xunit;

namespace RealEstateCRM.Tests.Auth;

public class HangfireDashboardAuthorizationFilterTests
{
    private static HttpContext CreateHttpContext(IPAddress? remoteIp = null, string? authorizationHeader = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remoteIp;
        if (authorizationHeader is not null)
        {
            context.Request.Headers["Authorization"] = authorizationHeader;
        }
        return context;
    }

    private static string BasicAuthHeader(string username, string password) =>
        "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

    [Fact]
    public void AuthorizeHttpContext_AllowsLoopback_WhenNoCredentialsConfigured()
    {
        var filter = new HangfireDashboardAuthorizationFilter(null, null);
        var context = CreateHttpContext(IPAddress.Loopback);

        Assert.True(filter.AuthorizeHttpContext(context));
    }

    [Fact]
    public void AuthorizeHttpContext_DeniesNonLoopback_WhenNoCredentialsConfigured()
    {
        var filter = new HangfireDashboardAuthorizationFilter(null, null);
        var context = CreateHttpContext(IPAddress.Parse("203.0.113.5"));

        Assert.False(filter.AuthorizeHttpContext(context));
    }

    [Fact]
    public void AuthorizeHttpContext_AllowsCorrectBasicAuthCredentials()
    {
        var filter = new HangfireDashboardAuthorizationFilter("admin", "s3cret!");
        var context = CreateHttpContext(IPAddress.Parse("203.0.113.5"), BasicAuthHeader("admin", "s3cret!"));

        Assert.True(filter.AuthorizeHttpContext(context));
    }

    [Fact]
    public void AuthorizeHttpContext_DeniesWrongPassword_AndSendsChallenge()
    {
        var filter = new HangfireDashboardAuthorizationFilter("admin", "s3cret!");
        var context = CreateHttpContext(IPAddress.Parse("203.0.113.5"), BasicAuthHeader("admin", "wrong"));

        var result = filter.AuthorizeHttpContext(context);

        Assert.False(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.True(context.Response.Headers.ContainsKey("WWW-Authenticate"));
    }

    [Fact]
    public void AuthorizeHttpContext_DeniesMissingAuthorizationHeader_WhenCredentialsConfigured()
    {
        var filter = new HangfireDashboardAuthorizationFilter("admin", "s3cret!");
        var context = CreateHttpContext(IPAddress.Loopback);

        // Even a loopback request must authenticate once credentials are configured — the
        // local-only fallback only applies when nothing is configured at all.
        Assert.False(filter.AuthorizeHttpContext(context));
    }

    [Fact]
    public void AuthorizeHttpContext_DeniesMalformedAuthorizationHeader()
    {
        var filter = new HangfireDashboardAuthorizationFilter("admin", "s3cret!");
        var context = CreateHttpContext(IPAddress.Loopback, "Basic not-valid-base64!!!");

        Assert.False(filter.AuthorizeHttpContext(context));
    }
}
