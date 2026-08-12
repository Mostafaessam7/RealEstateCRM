using System.Net;
using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace RealEstateCRM.Infrastructure.Auth;

/// <summary>
/// Guards /hangfire. When Hangfire:DashboardUsername/DashboardPassword are configured, requires
/// HTTP Basic Auth against them (constant-time comparison) — set these before deploying
/// anywhere network-reachable. When left unset (local dev default), falls back to
/// local-requests-only, same as Hangfire's built-in LocalRequestsOnlyAuthorizationFilter.
/// The Hangfire-facing Authorize(DashboardContext) is a thin adapter over AuthorizeHttpContext,
/// which is the pure, independently-testable core (no Hangfire types required to unit test it).
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string? _username;
    private readonly string? _password;

    public HangfireDashboardAuthorizationFilter(string? username, string? password)
    {
        _username = username;
        _password = password;
    }

    public bool Authorize(DashboardContext context) => AuthorizeHttpContext(context.GetHttpContext());

    public bool AuthorizeHttpContext(HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(_password))
        {
            return httpContext.Connection.RemoteIpAddress is not null &&
                IPAddress.IsLoopback(httpContext.Connection.RemoteIpAddress);
        }

        var header = httpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (header is not null && header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..].Trim()));
                var separatorIndex = decoded.IndexOf(':');

                if (separatorIndex > 0)
                {
                    var providedUsername = decoded[..separatorIndex];
                    var providedPassword = decoded[(separatorIndex + 1)..];

                    if (ConstantTimeEquals(providedUsername, _username) && ConstantTimeEquals(providedPassword, _password))
                    {
                        return true;
                    }
                }
            }
            catch (FormatException)
            {
                // Malformed Authorization header — fall through to the 401 challenge below.
            }
        }

        httpContext.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return false;
    }

    private static bool ConstantTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
