using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Projects;
using RealEstateCRM.Application.Projects.DTOs;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Projects;

public class ProjectService : IProjectService
{
    private static readonly HashSet<string> SortableFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreatedAt", "Name", "Status", "StartingPrice"
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;

    public ProjectService(ApplicationDbContext db, ICurrentTenantService currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<PagedResult<ProjectDto>> ListAsync(ProjectListQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var projects = _db.Projects.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            projects = projects.Where(p => EF.Functions.Like(p.Name, $"%{term}%"));
        }

        if (query.Status.HasValue)
        {
            projects = projects.Where(p => p.Status == query.Status.Value);
        }

        var sortBy = SortableFields.Contains(query.SortBy ?? string.Empty) ? query.SortBy! : "CreatedAt";
        var descending = !string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        projects = (sortBy, descending) switch
        {
            ("Name", true) => projects.OrderByDescending(p => p.Name),
            ("Name", false) => projects.OrderBy(p => p.Name),
            ("Status", true) => projects.OrderByDescending(p => p.Status),
            ("Status", false) => projects.OrderBy(p => p.Status),
            ("StartingPrice", true) => projects.OrderByDescending(p => p.StartingPrice),
            ("StartingPrice", false) => projects.OrderBy(p => p.StartingPrice),
            (_, true) => projects.OrderByDescending(p => p.CreatedAt),
            _ => projects.OrderBy(p => p.CreatedAt)
        };

        var totalCount = await projects.CountAsync(cancellationToken);
        var items = await projects.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<ProjectDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProjectDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new AppException("Project not found.", 404);

        return ToDto(project);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Developer = request.Developer,
            Location = request.Location,
            Description = request.Description,
            StartingPrice = request.StartingPrice,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(project);
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new AppException("Project not found.", 404);

        project.Name = request.Name.Trim();
        project.Developer = request.Developer;
        project.Location = request.Location;
        project.Description = request.Description;
        project.StartingPrice = request.StartingPrice;
        project.Status = request.Status;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(project);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new AppException("Project not found.", 404);

        project.IsDeleted = true;
        project.DeletedAt = DateTime.UtcNow;
        project.DeletedBy = _currentTenant.UserId;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static ProjectDto ToDto(Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Developer = project.Developer,
        Location = project.Location,
        Description = project.Description,
        StartingPrice = project.StartingPrice,
        Status = project.Status,
        CreatedAt = project.CreatedAt,
        UpdatedAt = project.UpdatedAt
    };
}
