using System.Security.Cryptography;

namespace RealEstateCRM.Api.Auth;

/// <summary>
/// Optional cookie-based transport for the web SPA's refresh token, additive to the existing
/// JSON-body flow (which Flutter and Public API/third-party integrations keep using entirely
/// unchanged). A caller opts in by sending "X-Auth-Transport: cookie" on login/refresh/logout —
/// see docs/auth.md#web-cookie-auth for the full design and why it's scoped to the web client
/// only (native apps have OS-level secure storage; cookies are a browser-specific mitigation
/// for a browser-specific threat, storing a JWT/refresh token in localStorage).
/// </summary>
public static class WebAuthCookies
{
    public const string RefreshTokenCookieName = "rt";
    public const string CsrfCookieName = "XSRF-TOKEN";
    public const string CsrfHeaderName = "X-CSRF-Token";
    public const string TransportHeaderName = "X-Auth-Transport";
    private const string AuthPath = "/api/auth";

    public static bool WantsCookieTransport(HttpRequest request) =>
        request.Headers[TransportHeaderName].ToString().Equals("cookie", StringComparison.OrdinalIgnoreCase)
        || request.Cookies.ContainsKey(RefreshTokenCookieName);

    public static string GenerateCsrfToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    /// <summary>Constant-time comparison — this is a security check, not a display value.</summary>
    public static bool CsrfHeaderMatchesCookie(HttpRequest request)
    {
        var headerValue = request.Headers[CsrfHeaderName].ToString();
        var cookieValue = request.Cookies[CsrfCookieName];

        if (string.IsNullOrEmpty(headerValue) || string.IsNullOrEmpty(cookieValue))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(headerValue),
            System.Text.Encoding.UTF8.GetBytes(cookieValue));
    }

    public static void SetAuthCookies(HttpResponse response, string refreshToken, DateTime refreshTokenExpiresAt, bool isDevelopment)
    {
        // SameSite=None is required for the web app and API to live on different origins
        // (separate Azure Web Apps per docs/deployment.md) — browsers reject SameSite=None
        // without Secure, and plain HTTP local dev has no TLS, so Development uses Lax+non-Secure
        // instead: ports don't affect the SameSite "site" definition, so Lax still works fine
        // between e.g. http://localhost:5173 and http://localhost:5063.
        var sameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None;
        var secure = !isDevelopment;

        response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = AuthPath,
            Expires = refreshTokenExpiresAt,
        });

        // Deliberately NOT HttpOnly — the SPA must be able to read this and echo it back as a
        // header (double-submit pattern). Its security property comes from same-origin JS being
        // the only thing that can read it, not from secrecy against the server.
        response.Cookies.Append(CsrfCookieName, GenerateCsrfToken(), new CookieOptions
        {
            HttpOnly = false,
            Secure = secure,
            SameSite = sameSite,
            Path = "/",
            Expires = refreshTokenExpiresAt,
        });
    }

    public static void ClearAuthCookies(HttpResponse response, bool isDevelopment)
    {
        var sameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None;
        var secure = !isDevelopment;

        response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = AuthPath, Secure = secure, SameSite = sameSite });
        response.Cookies.Delete(CsrfCookieName, new CookieOptions { Path = "/", Secure = secure, SameSite = sameSite });
    }
}
