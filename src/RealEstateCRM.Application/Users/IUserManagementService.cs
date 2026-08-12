using RealEstateCRM.Application.Users.DTOs;

namespace RealEstateCRM.Application.Users;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>CompanyAdmin/SuperAdmin only.</summary>
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>CompanyAdmin/SuperAdmin only.</summary>
    Task<UserDto> UpdateRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>CompanyAdmin/SuperAdmin only.</summary>
    Task<UserDto> UpdateActiveAsync(Guid userId, UpdateUserActiveRequest request, CancellationToken cancellationToken = default);
}
