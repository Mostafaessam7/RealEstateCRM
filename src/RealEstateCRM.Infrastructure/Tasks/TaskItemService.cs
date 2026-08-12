using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Tasks;
using RealEstateCRM.Application.Tasks.DTOs;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Tasks;

public class TaskItemService : ITaskItemService
{
    private static readonly HashSet<string> SortableFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreatedAt", "DueAt", "Priority", "Status"
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;

    public TaskItemService(ApplicationDbContext db, ICurrentTenantService currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<PagedResult<TaskItemDto>> ListAsync(TaskItemListQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var tasks = _db.Tasks.AsNoTracking().AsQueryable();

        if (query.Status.HasValue)
        {
            tasks = tasks.Where(t => t.Status == query.Status.Value);
        }

        if (query.AssignedToUserId.HasValue)
        {
            tasks = tasks.Where(t => t.AssignedToUserId == query.AssignedToUserId.Value);
        }

        if (query.LeadId.HasValue)
        {
            tasks = tasks.Where(t => t.LeadId == query.LeadId.Value);
        }

        if (query.DealId.HasValue)
        {
            tasks = tasks.Where(t => t.DealId == query.DealId.Value);
        }

        var sortBy = SortableFields.Contains(query.SortBy ?? string.Empty) ? query.SortBy! : "DueAt";
        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        tasks = (sortBy, descending) switch
        {
            ("CreatedAt", true) => tasks.OrderByDescending(t => t.CreatedAt),
            ("CreatedAt", false) => tasks.OrderBy(t => t.CreatedAt),
            ("Priority", true) => tasks.OrderByDescending(t => t.Priority),
            ("Priority", false) => tasks.OrderBy(t => t.Priority),
            ("Status", true) => tasks.OrderByDescending(t => t.Status),
            ("Status", false) => tasks.OrderBy(t => t.Status),
            (_, true) => tasks.OrderByDescending(t => t.DueAt),
            _ => tasks.OrderBy(t => t.DueAt)
        };

        var totalCount = await tasks.CountAsync(cancellationToken);
        var items = await tasks.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<TaskItemDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<TaskItemDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new AppException("Task not found.", 404);

        return ToDto(task);
    }

    public async Task<TaskItemDto> CreateAsync(CreateTaskItemRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureAssigneeValidAsync(request.AssignedToUserId, cancellationToken);
        await EnsureLeadValidAsync(request.LeadId, cancellationToken);
        await EnsureDealValidAsync(request.DealId, cancellationToken);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description,
            AssignedToUserId = request.AssignedToUserId,
            LeadId = request.LeadId,
            DealId = request.DealId,
            DueAt = request.DueAt,
            Priority = request.Priority,
            Status = TaskItemStatus.Pending,
            ReminderAt = request.ReminderAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(task);
    }

    public async Task<TaskItemDto> UpdateAsync(Guid id, UpdateTaskItemRequest request, CancellationToken cancellationToken = default)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new AppException("Task not found.", 404);

        await EnsureLeadValidAsync(request.LeadId, cancellationToken);
        await EnsureDealValidAsync(request.DealId, cancellationToken);

        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.LeadId = request.LeadId;
        task.DealId = request.DealId;
        task.DueAt = request.DueAt;
        task.Priority = request.Priority;
        task.ReminderAt = request.ReminderAt;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(task);
    }

    public async Task<TaskItemDto> AssignAsync(Guid id, AssignTaskItemRequest request, CancellationToken cancellationToken = default)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new AppException("Task not found.", 404);

        await EnsureAssigneeValidAsync(request.AssignedToUserId, cancellationToken);

        task.AssignedToUserId = request.AssignedToUserId;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(task);
    }

    public async Task<TaskItemDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new AppException("Task not found.", 404);

        if (task.Status != TaskItemStatus.Pending)
        {
            throw new AppException("Only a Pending task can be completed.", 400);
        }

        task.Status = TaskItemStatus.Completed;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(task);
    }

    public async Task<TaskItemDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new AppException("Task not found.", 404);

        if (task.Status != TaskItemStatus.Pending)
        {
            throw new AppException("Only a Pending task can be cancelled.", 400);
        }

        task.Status = TaskItemStatus.Cancelled;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(task);
    }

    private async Task EnsureAssigneeValidAsync(Guid userId, CancellationToken cancellationToken)
    {
        var exists = await _db.Users.AnyAsync(u => u.Id == userId && u.CompanyId == _currentTenant.CompanyId, cancellationToken);
        if (!exists)
        {
            throw new AppException("Assignee not found in this company.", 400);
        }
    }

    private async Task EnsureLeadValidAsync(Guid? leadId, CancellationToken cancellationToken)
    {
        if (!leadId.HasValue)
        {
            return;
        }

        var exists = await _db.Leads.AnyAsync(l => l.Id == leadId.Value, cancellationToken);
        if (!exists)
        {
            throw new AppException("Lead not found.", 400);
        }
    }

    private async Task EnsureDealValidAsync(Guid? dealId, CancellationToken cancellationToken)
    {
        if (!dealId.HasValue)
        {
            return;
        }

        var exists = await _db.Deals.AnyAsync(d => d.Id == dealId.Value, cancellationToken);
        if (!exists)
        {
            throw new AppException("Deal not found.", 400);
        }
    }

    private static TaskItemDto ToDto(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        AssignedToUserId = task.AssignedToUserId,
        LeadId = task.LeadId,
        DealId = task.DealId,
        DueAt = task.DueAt,
        Priority = task.Priority,
        Status = task.Status,
        ReminderAt = task.ReminderAt,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };
}
