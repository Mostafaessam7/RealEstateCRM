using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Documents;
using RealEstateCRM.Application.Documents.DTOs;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> List(
        [FromQuery] Guid? leadId, [FromQuery] Guid? dealId, CancellationToken cancellationToken)
    {
        return Ok(await _documentService.ListAsync(leadId, dealId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<DocumentDto>> Upload(
        IFormFile file, [FromForm] Guid? leadId, [FromForm] Guid? dealId, CancellationToken cancellationToken)
    {
        var request = new UploadDocumentRequest { LeadId = leadId, DealId = dealId };

        await using var stream = file.OpenReadStream();
        var document = await _documentService.UploadAsync(
            request, stream, file.FileName, file.ContentType, file.Length, cancellationToken);

        return Ok(document);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _documentService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
