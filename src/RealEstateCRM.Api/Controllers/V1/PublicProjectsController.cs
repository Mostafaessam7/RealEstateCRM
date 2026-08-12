using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Projects;
using RealEstateCRM.Application.Projects.DTOs;

namespace RealEstateCRM.Api.Controllers.V1;

[Route("api/v1/projects")]
public class PublicProjectsController : PublicApiControllerBase
{
    private readonly IProjectService _projectService;

    public PublicProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProjectDto>>> List([FromQuery] ProjectListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _projectService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _projectService.GetByIdAsync(id, cancellationToken));
    }
}
