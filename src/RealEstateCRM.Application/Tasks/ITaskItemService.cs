using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Tasks.DTOs;

namespace RealEstateCRM.Application.Tasks;

public interface ITaskItemService
{
    Task<PagedResult<TaskItemDto>> ListAsync(TaskItemListQuery query, CancellationToken cancellationToken = default);

    Task<TaskItemDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TaskItemDto> CreateAsync(CreateTaskItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>Field edits only — not status/assignee.</summary>
    Task<TaskItemDto> UpdateAsync(Guid id, UpdateTaskItemRequest request, CancellationToken cancellationToken = default);

    Task<TaskItemDto> AssignAsync(Guid id, AssignTaskItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>-> Completed.</summary>
    Task<TaskItemDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>-> Cancelled.</summary>
    Task<TaskItemDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
