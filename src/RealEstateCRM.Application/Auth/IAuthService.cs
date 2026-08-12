using RealEstateCRM.Application.Auth.DTOs;

namespace RealEstateCRM.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
