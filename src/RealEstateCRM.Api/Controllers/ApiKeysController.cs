using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.ApiKeys;
using RealEstateCRM.Application.ApiKeys.DTOs;
using RealEstateCRM.Application.Common.Validation;
using RealEstateCRM.Domain.Constants;

namespace RealEstateCRM.Api.Controllers;

/// <summary>Manages credentials for the Public API (see docs/public-api.md).</summary>
[ApiController]
[Authorize(Roles = $"{Roles.CompanyAdmin},{Roles.SuperAdmin}")]
[Route("api/api-keys")]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly IValidator<CreateApiKeyRequest> _createValidator;

    public ApiKeysController(IApiKeyService apiKeyService, IValidator<CreateApiKeyRequest> createValidator)
    {
        _apiKeyService = apiKeyService;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApiKeyDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _apiKeyService.ListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CreatedApiKeyDto>> Create(CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _apiKeyService.CreateAsync(request, cancellationToken));
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        await _apiKeyService.RevokeAsync(id, cancellationToken);
        return NoContent();
    }
}
