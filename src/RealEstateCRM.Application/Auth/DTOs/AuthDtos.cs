using System.ComponentModel.DataAnnotations;

namespace RealEstateCRM.Application.Auth.DTOs;

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RefreshRequest
{
    /// <summary>
    /// Optional — only required for non-browser callers (Flutter, Public API integrations)
    /// that don't use the httpOnly refresh-token cookie. A browser (web) caller sends the
    /// X-Auth-Transport: cookie header instead and omits this entirely; AuthController reads
    /// the refresh token from the "rt" cookie in that case. See docs/auth.md#web-cookie-auth.
    /// </summary>
    public string? RefreshToken { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}
