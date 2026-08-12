using Microsoft.AspNetCore.Http;
using RealEstateCRM.Api.Auth;
using Xunit;

namespace RealEstateCRM.Tests.Auth;

public class WebAuthCookiesTests
{
    [Fact]
    public void WantsCookieTransport_TrueWhenHeaderPresent()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[WebAuthCookies.TransportHeaderName] = "cookie";

        Assert.True(WebAuthCookies.WantsCookieTransport(context.Request));
    }

    [Fact]
    public void WantsCookieTransport_TrueWhenRefreshCookiePresentEvenWithoutHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("Cookie", $"{WebAuthCookies.RefreshTokenCookieName}=some-token");

        Assert.True(WebAuthCookies.WantsCookieTransport(context.Request));
    }

    [Fact]
    public void WantsCookieTransport_FalseForAPlainBodyOnlyRequest()
    {
        var context = new DefaultHttpContext();

        Assert.False(WebAuthCookies.WantsCookieTransport(context.Request));
    }

    [Fact]
    public void GenerateCsrfToken_ProducesDifferentValuesEachCall()
    {
        var a = WebAuthCookies.GenerateCsrfToken();
        var b = WebAuthCookies.GenerateCsrfToken();

        Assert.NotEqual(a, b);
        Assert.True(a.Length > 20); // 32 random bytes, base64-encoded
    }

    [Fact]
    public void CsrfHeaderMatchesCookie_TrueWhenHeaderAndCookieMatch()
    {
        var context = new DefaultHttpContext();
        var token = WebAuthCookies.GenerateCsrfToken();
        context.Request.Headers[WebAuthCookies.CsrfHeaderName] = token;
        context.Request.Headers.Append("Cookie", $"{WebAuthCookies.CsrfCookieName}={Uri.EscapeDataString(token)}");

        Assert.True(WebAuthCookies.CsrfHeaderMatchesCookie(context.Request));
    }

    [Fact]
    public void CsrfHeaderMatchesCookie_FalseWhenTheyDiffer()
    {
        // The core CSRF defense: an attacker's cross-site page cannot read the victim's
        // CSRF cookie (same-origin policy), so it cannot produce a header that matches it.
        var context = new DefaultHttpContext();
        context.Request.Headers[WebAuthCookies.CsrfHeaderName] = "attacker-guess";
        context.Request.Headers.Append("Cookie", $"{WebAuthCookies.CsrfCookieName}={WebAuthCookies.GenerateCsrfToken()}");

        Assert.False(WebAuthCookies.CsrfHeaderMatchesCookie(context.Request));
    }

    [Fact]
    public void CsrfHeaderMatchesCookie_FalseWhenHeaderMissing()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("Cookie", $"{WebAuthCookies.CsrfCookieName}={WebAuthCookies.GenerateCsrfToken()}");

        Assert.False(WebAuthCookies.CsrfHeaderMatchesCookie(context.Request));
    }

    [Fact]
    public void CsrfHeaderMatchesCookie_FalseWhenCookieMissing()
    {
        // A plain cross-site form/simple-request "CSRF" attempt with no cookie at all.
        var context = new DefaultHttpContext();
        context.Request.Headers[WebAuthCookies.CsrfHeaderName] = "anything";

        Assert.False(WebAuthCookies.CsrfHeaderMatchesCookie(context.Request));
    }

    [Fact]
    public void SetAuthCookies_InDevelopment_UsesLaxAndNonSecure()
    {
        var context = new DefaultHttpContext();

        WebAuthCookies.SetAuthCookies(context.Response, "refresh-token-value", DateTime.UtcNow.AddDays(7), isDevelopment: true);

        var setCookieHeaders = context.Response.Headers.SetCookie.Where(h => h is not null).Select(h => h!).ToArray();
        Assert.Contains(setCookieHeaders, h => h.Contains($"{WebAuthCookies.RefreshTokenCookieName}=") && h.Contains("httponly", StringComparison.OrdinalIgnoreCase) && h.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase) && !h.Contains("secure", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setCookieHeaders, h => h.Contains($"{WebAuthCookies.CsrfCookieName}=") && !h.Contains("httponly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SetAuthCookies_InProduction_UsesNoneAndSecure()
    {
        var context = new DefaultHttpContext();

        WebAuthCookies.SetAuthCookies(context.Response, "refresh-token-value", DateTime.UtcNow.AddDays(7), isDevelopment: false);

        var setCookieHeaders = context.Response.Headers.SetCookie.Where(h => h is not null).Select(h => h!).ToArray();
        Assert.Contains(setCookieHeaders, h => h.Contains($"{WebAuthCookies.RefreshTokenCookieName}=") && h.Contains("secure", StringComparison.OrdinalIgnoreCase) && h.Contains("samesite=none", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SetAuthCookies_RefreshCookieIsScopedToAuthPathOnly()
    {
        var context = new DefaultHttpContext();

        WebAuthCookies.SetAuthCookies(context.Response, "refresh-token-value", DateTime.UtcNow.AddDays(7), isDevelopment: false);

        var setCookieHeaders = context.Response.Headers.SetCookie.Where(h => h is not null).Select(h => h!).ToArray();
        Assert.Contains(setCookieHeaders, h => h.Contains($"{WebAuthCookies.RefreshTokenCookieName}=") && h.Contains("path=/api/auth", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ClearAuthCookies_ExpiresBothCookies()
    {
        var context = new DefaultHttpContext();

        WebAuthCookies.ClearAuthCookies(context.Response, isDevelopment: false);

        var setCookieHeaders = context.Response.Headers.SetCookie.Where(h => h is not null).Select(h => h!).ToArray();
        Assert.Contains(setCookieHeaders, h => h.StartsWith($"{WebAuthCookies.RefreshTokenCookieName}=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setCookieHeaders, h => h.StartsWith($"{WebAuthCookies.CsrfCookieName}=", StringComparison.OrdinalIgnoreCase));
    }
}
