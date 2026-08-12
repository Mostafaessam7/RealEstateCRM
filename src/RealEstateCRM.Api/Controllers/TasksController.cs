using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Common.Validation;
using RealEstateCRM.Application.Tasks;
using RealEstateCRM.Application.Tasks.DTOs;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskItemService _taskService;
    private readonly IValidator<CreateTaskItemRequest> _createValidator;
    private readonly IValidator<UpdateTaskItemRequest> _updateValidator;
    private readonly IValidator<AssignTaskItemRequest> _assignValidator;

    public TasksController(
        ITaskItemService taskService,
        IValidator<CreateTaskItemRequest> createValidator,
        IValidator<UpdateTaskItemRequest> updateValidator,
        IValidator<AssignTaskItemRequest> assignValidator)
    {
        _taskService = taskService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _assignValidator = assignValidator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskItemDto>>> List([FromQuery] TaskItemListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskItemDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<TaskItemDto>> Create(CreateTaskItemRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        var task = await _taskService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskItemDto>> Update(Guid id, UpdateTaskItemRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _taskService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<TaskItemDto>> Assign(Guid id, AssignTaskItemRequest request, CancellationToken cancellationToken)
    {
        await _assignValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _taskService.AssignAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<TaskItemDto>> Complete(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.CompleteAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<TaskItemDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.CancelAsync(id, cancellationToken));
    }
}
