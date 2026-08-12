using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Common.Validation;
using RealEstateCRM.Application.Units;
using RealEstateCRM.Application.Units.DTOs;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/units")]
public class UnitsController : ControllerBase
{
    private readonly IUnitService _unitService;
    private readonly IUnitImageService _unitImageService;
    private readonly IValidator<CreateUnitRequest> _createValidator;
    private readonly IValidator<UpdateUnitRequest> _updateValidator;

    public UnitsController(
        IUnitService unitService,
        IUnitImageService unitImageService,
        IValidator<CreateUnitRequest> createValidator,
        IValidator<UpdateUnitRequest> updateValidator)
    {
        _unitService = unitService;
        _unitImageService = unitImageService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UnitDto>>> List([FromQuery] UnitListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _unitService.ListAsync(query, cancellationToken));
    }

    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<UnitDto>>> GetAvailable([FromQuery] Guid? projectId, CancellationToken cancellationToken)
    {
        return Ok(await _unitService.GetAvailableAsync(projectId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _unitService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<UnitDto>> Create(CreateUnitRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        var unit = await _unitService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = unit.Id }, unit);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UnitDto>> Update(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _unitService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _unitService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/images")]
    public async Task<ActionResult<IReadOnlyList<UnitImageDto>>> ListImages(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _unitImageService.ListAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/images")]
    public async Task<ActionResult<UnitImageDto>> UploadImage(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var image = await _unitImageService.UploadAsync(id, stream, file.FileName, file.ContentType, file.Length, cancellationToken);
        return Ok(image);
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        await _unitImageService.DeleteAsync(id, imageId, cancellationToken);
        return NoContent();
    }
}
