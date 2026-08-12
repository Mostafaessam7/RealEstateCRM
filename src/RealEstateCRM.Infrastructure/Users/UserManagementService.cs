using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Subscriptions;
using RealEstateCRM.Application.Users;
using RealEstateCRM.Application.Users.DTOs;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Subscriptions;

namespace RealEstateCRM.Infrastructure.Users;

public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentTenantService _currentTenant;
    private readonly ISubscriptionLimitService _subscriptionLimit;

    public UserManagementService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ICurrentTenantService currentTenant,
        ISubscriptionLimitService? subscriptionLimit = null)
    {
        _db = db;
        _userManager = userManager;
        _currentTenant = currentTenant;
        _subscriptionLimit = subscriptionLimit ?? NullSubscriptionLimitService.Instance;
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var companyId = _currentTenant.CompanyId
            ?? throw new AppException("Authenticated company context is required.", 401);

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.CompanyId == companyId)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

        var dtos = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            dtos.Add(await ToDtoAsync(user));
        }

        return dtos;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        await _subscriptionLimit.EnsureCanAddUserAsync(cancellationToken);

        var companyId = _currentTenant.CompanyId
            ?? throw new AppException("Authenticated company context is required.", 401);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FullName = request.FullName.Trim(),
            Email = request.Email,
            UserName = request.Email,
            ManagerId = request.ManagerId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            throw new AppException(string.Join(" ", createResult.Errors.Select(e => e.Description)), 400);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            throw new AppException(string.Join(" ", roleResult.Errors.Select(e => e.Description)), 400);
        }

        return await ToDtoAsync(user);
    }

    public async Task<UserDto> UpdateRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindInCompanyAsync(userId);

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        var result = await _userManager.AddToRoleAsync(user, request.Role);
        if (!result.Succeeded)
        {
            throw new AppException(string.Join(" ", result.Errors.Select(e => e.Description)), 400);
        }

        return await ToDtoAsync(user);
    }

    public async Task<UserDto> UpdateActiveAsync(Guid userId, UpdateUserActiveRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindInCompanyAsync(userId);

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(user);
    }

    private async Task<ApplicationUser> FindInCompanyAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == _currentTenant.CompanyId)
            ?? throw new AppException("User not found.", 404);

        return user;
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList(),
            IsActive = user.IsActive,
            ManagerId = user.ManagerId,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt
        };
    }
}
