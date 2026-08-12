using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Common.Validation;
using RealEstateCRM.Application.Users;
using RealEstateCRM.Application.Users.DTOs;
using RealEstateCRM.Domain.Constants;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserAvatarService _userAvatarService;
    private readonly IUserManagementService _userManagementService;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRoleRequest> _updateRoleValidator;

    public UsersController(
        IUserAvatarService userAvatarService,
        IUserManagementService userManagementService,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRoleRequest> updateRoleValidator)
    {
        _userAvatarService = userAvatarService;
        _userManagementService = userManagementService;
        _createValidator = createValidator;
        _updateRoleValidator = updateRoleValidator;
    }

    [HttpPost("me/avatar")]
    public async Task<ActionResult<object>> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var url = await _userAvatarService.UploadAvatarAsync(
            GetUserId(), stream, file.FileName, file.ContentType, file.Length, cancellationToken);

        return Ok(new { avatarUrl = url });
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _userManagementService.ListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.CompanyAdmin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _userManagementService.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:guid}/role")]
    [Authorize(Roles = $"{Roles.CompanyAdmin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<UserDto>> UpdateRole(Guid id, UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        await _updateRoleValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _userManagementService.UpdateRoleAsync(id, request, cancellationToken));
    }

    [HttpPut("{id:guid}/active")]
    [Authorize(Roles = $"{Roles.CompanyAdmin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<UserDto>> UpdateActive(Guid id, UpdateUserActiveRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _userManagementService.UpdateActiveAsync(id, request, cancellationToken));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("User id claim missing."));
}
