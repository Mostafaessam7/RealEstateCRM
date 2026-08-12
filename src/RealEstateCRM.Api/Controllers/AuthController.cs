using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using RealEstateCRM.Api.Auth;
using RealEstateCRM.Application.Auth;
using RealEstateCRM.Application.Auth.DTOs;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Infrastructure.Auth;

namespace RealEstateCRM.Api.Controllers;

/// <summary>
/// Two auth transports are supported, chosen per-request by the caller — see
/// docs/auth.md#web-cookie-auth:
/// - JSON body (unchanged, original behavior): Flutter and any Public API/third-party
///   integration. AuthResponse.RefreshToken is populated as always.
/// - httpOnly cookie (opt-in via "X-Auth-Transport: cookie"): the web SPA. The refresh token
///   never appears in a JSON response body at all in this mode — it only ever exists in the
///   httpOnly cookie — so it's unreachable even to an XSS payload that hooks fetch/XHR to read
///   response bodies, not just one that reads localStorage.
/// </summary>
[ApiController]
[Route("api/auth")]
[EnableRateLimiting("Auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtOptions _jwtOptions;
    private readonly IWebHostEnvironment _environment;

    public AuthController(IAuthService authService, IOptions<JwtOptions> jwtOptions, IWebHostEnvironment environment)
    {
        _authService = authService;
        _jwtOptions = jwtOptions.Value;
        _environment = environment;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, GetClientIp(), cancellationToken);
        return Ok(ApplyCookieTransportIfRequested(result));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var refreshToken = ResolveRefreshToken(request, requireCsrf: true, out var isCookieTransport);
        var result = await _authService.RefreshAsync(refreshToken, GetClientIp(), cancellationToken);
        return Ok(isCookieTransport ? ApplyCookieTransport(result) : result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        // Logout is not a state-changing action an attacker gains anything from forging (worst
        // case: it logs the victim's own session out), so no CSRF check is required here.
        var refreshToken = ResolveRefreshToken(request, requireCsrf: false, out var isCookieTransport);

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.LogoutAsync(refreshToken, cancellationToken);
        }

        if (isCookieTransport)
        {
            WebAuthCookies.ClearAuthCookies(Response, _environment.IsDevelopment());
        }

        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ChangePasswordAsync(GetUserId(), request, cancellationToken);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(request.Email, cancellationToken);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Login has no existing cookie to detect transport from, so it relies solely on the opt-in header.</summary>
    private AuthResponse ApplyCookieTransportIfRequested(AuthResponse result) =>
        WebAuthCookies.WantsCookieTransport(Request) ? ApplyCookieTransport(result) : result;

    private AuthResponse ApplyCookieTransport(AuthResponse result)
    {
        WebAuthCookies.SetAuthCookies(
            Response,
            result.RefreshToken,
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            _environment.IsDevelopment());

        // Never let the refresh token leave the server in a JSON body once it's cookie-carried —
        // otherwise it's still readable by any XSS payload that intercepts the fetch/XHR
        // response directly, defeating the point of HttpOnly.
        return new AuthResponse
        {
            AccessToken = result.AccessToken,
            AccessTokenExpiresAt = result.AccessTokenExpiresAt,
            RefreshToken = string.Empty,
        };
    }

    /// <summary>
    /// Resolves which refresh token to act on and whether this is a cookie-transport request,
    /// enforcing the CSRF double-submit check when required and the token came from a cookie.
    /// </summary>
    private string ResolveRefreshToken(RefreshRequest request, bool requireCsrf, out bool isCookieTransport)
    {
        var cookieToken = Request.Cookies[WebAuthCookies.RefreshTokenCookieName];
        if (!string.IsNullOrEmpty(cookieToken))
        {
            isCookieTransport = true;
            if (requireCsrf && !WebAuthCookies.CsrfHeaderMatchesCookie(Request))
            {
                throw new AppException("Missing or invalid CSRF token.", 403);
            }
            return cookieToken;
        }

        isCookieTransport = false;
        return request.RefreshToken ?? string.Empty;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("User id claim missing."));

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
