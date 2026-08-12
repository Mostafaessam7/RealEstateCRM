using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RealEstateCRM.Application.Auth;
using RealEstateCRM.Application.Auth.DTOs;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailSender _emailSender;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IJwtTokenGenerator jwtTokenGenerator,
        IEmailSender emailSender,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailSender = emailSender;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new AppException("Invalid email or password.", 401);
        }

        await EnsureCompanyActiveAsync(user, cancellationToken);

        return await IssueTokensAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var existing = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existing is null || !existing.IsActive)
        {
            throw new AppException("Invalid or expired refresh token.", 401);
        }

        var user = await _userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            throw new AppException("Invalid or expired refresh token.", 401);
        }

        await EnsureCompanyActiveAsync(user, cancellationToken);

        existing.RevokedAt = DateTime.UtcNow;
        existing.RevokedByIp = ipAddress;

        var response = await IssueTokensAsync(user, ipAddress, cancellationToken, existing);
        return response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var existing = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existing is not null && existing.IsActive)
        {
            existing.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new AppException("User not found.", 404);

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new AppException(string.Join(" ", result.Errors.Select(e => e.Description)), 400);
        }

        // A stolen refresh token must not outlive a password change — otherwise an attacker who
        // captured one keeps access indefinitely even after the legitimate user "secures" the
        // account. Every other device/session is forced to log in again.
        await RevokeAllActiveRefreshTokensAsync(user.Id, cancellationToken);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            // Do not reveal whether the account exists.
            return;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        await _emailSender.SendAsync(email, "Reset your password", $"Password reset token: {token}", cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new AppException("Invalid request.", 400);

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new AppException(string.Join(" ", result.Errors.Select(e => e.Description)), 400);
        }

        // Same reasoning as ChangePasswordAsync — a forgot-password reset is often triggered
        // specifically because a session/device is suspected compromised, so it must not leave
        // any existing refresh token still usable.
        await RevokeAllActiveRefreshTokensAsync(user.Id, cancellationToken);
    }

    private async Task RevokeAllActiveRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        if (activeTokens.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureCompanyActiveAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (!user.CompanyId.HasValue)
        {
            return; // platform-level user (SuperAdmin)
        }

        var companyIsActive = await _dbContext.Companies
            .Where(c => c.Id == user.CompanyId.Value)
            .Select(c => c.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (!companyIsActive)
        {
            throw new AppException("Company account is inactive.", 401);
        }
    }

    private async Task<AuthResponse> IssueTokensAsync(
        ApplicationUser user,
        string? ipAddress,
        CancellationToken cancellationToken,
        RefreshToken? replaces = null)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.FullName, user.CompanyId, roles);

        var refreshTokenPlainText = GenerateRefreshTokenValue();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(refreshTokenPlainText),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        _dbContext.RefreshTokens.Add(refreshToken);

        if (replaces is not null)
        {
            replaces.ReplacedByTokenId = refreshToken.Id;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = expiresAt,
            RefreshToken = refreshTokenPlainText
        };
    }

    private static string GenerateRefreshTokenValue() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
