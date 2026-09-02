using AppSupportHub.Application.WorkItems.Queries;
using AppSupportHub.Application.WorkItems.ReadModels;
using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;
using Microsoft.EntityFrameworkCore;

namespace AppSupportHub.Infrastructure.Persistence.Queries.WorkItems;

public sealed class WorkItemQueries : IWorkItemQueries
{
    private const string LikeEscapeCharacter = "\\";
    private readonly AppSupportHubDbContext _dbContext;

    public WorkItemQueries(AppSupportHubDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<WorkItemDetail?> GetByIdAsync(
        Guid id,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
    {
        DateTimeOffset normalizedAsOfUtc = asOfUtc.ToUniversalTime();
        WorkItemDetail? detail = await _dbContext.Set<WorkItem>()
            .AsNoTracking()
            .Where(workItem => workItem.Id == id)
            .Join(
                _dbContext.Set<ApplicationSystem>().AsNoTracking(),
                workItem => workItem.ApplicationSystemId,
                applicationSystem => applicationSystem.Id,
                (workItem, applicationSystem) => new WorkItemDetail(
                    workItem.Id,
                    workItem.ApplicationSystemId,
                    applicationSystem.Name,
                    workItem.Type,
                    workItem.Title,
                    workItem.Description,
                    workItem.Priority,
                    workItem.Status,
                    workItem.AssigneeIdentifier,
                    workItem.DueAtUtc,
                    workItem.CreatedAtUtc,
                    workItem.UpdatedAtUtc,
                    workItem.DueAtUtc != null
                        && workItem.DueAtUtc < normalizedAsOfUtc
                        && workItem.Status != WorkItemStatus.Resolved
                        && workItem.Status != WorkItemStatus.Closed
                        && workItem.Status != WorkItemStatus.Cancelled,
                    workItem.ResolutionSummary,
                    workItem.ResolvedAtUtc,
                    Array.Empty<WorkItemHistoryItem>()))
            .SingleOrDefaultAsync(cancellationToken);

        if (detail is null)
        {
            return null;
        }

        List<WorkItemHistoryItem> history = await _dbContext.Set<WorkItemHistoryEntry>()
            .AsNoTracking()
            .Where(historyEntry => historyEntry.WorkItemId == id)
            .OrderBy(historyEntry => EF.Property<int>(
                historyEntry,
                AppSupportHubDbContext.HistorySequencePropertyName))
            .Select(historyEntry => new WorkItemHistoryItem(
                historyEntry.EventType,
                historyEntry.ActorIdentifier,
                historyEntry.OccurredAtUtc,
                historyEntry.PreviousValue,
                historyEntry.NewValue,
                historyEntry.Comment))
            .ToListAsync(cancellationToken);

        return detail with { History = history };
    }

    public async Task<IReadOnlyList<WorkItemSummary>> ListAsync(
        WorkItemQueryFilter filter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        IQueryable<WorkItem> workItems = _dbContext.Set<WorkItem>().AsNoTracking();

        if (filter.ApplicationSystemId is not null)
        {
            workItems = workItems.Where(
                workItem => workItem.ApplicationSystemId == filter.ApplicationSystemId);
        }

        if (filter.TitleSearch is not null)
        {
            string titlePattern = $"%{EscapeLikePattern(filter.TitleSearch)}%";
            workItems = workItems.Where(workItem => EF.Functions.ILike(
                workItem.Title,
                titlePattern,
                LikeEscapeCharacter));
        }

        if (filter.Type is not null)
        {
            workItems = workItems.Where(workItem => workItem.Type == filter.Type);
        }

        if (filter.Priority is not null)
        {
            workItems = workItems.Where(workItem => workItem.Priority == filter.Priority);
        }

        if (filter.Status is not null)
        {
            workItems = workItems.Where(workItem => workItem.Status == filter.Status);
        }

        if (filter.AssigneeIdentifier is not null)
        {
            string assigneePattern = EscapeLikePattern(filter.AssigneeIdentifier);
            workItems = workItems.Where(workItem => workItem.AssigneeIdentifier != null
                && EF.Functions.ILike(
                    workItem.AssigneeIdentifier,
                    assigneePattern,
                    LikeEscapeCharacter));
        }

        DateTimeOffset normalizedAsOfUtc = filter.AsOfUtc.ToUniversalTime();

        if (filter.OverdueOnly)
        {
            workItems = workItems.Where(workItem => workItem.DueAtUtc != null
                && workItem.DueAtUtc < normalizedAsOfUtc
                && workItem.Status != WorkItemStatus.Resolved
                && workItem.Status != WorkItemStatus.Closed
                && workItem.Status != WorkItemStatus.Cancelled);
        }

        return await workItems
            .Join(
                _dbContext.Set<ApplicationSystem>().AsNoTracking(),
                workItem => workItem.ApplicationSystemId,
                applicationSystem => applicationSystem.Id,
                (workItem, applicationSystem) => new { workItem, applicationSystem.Name })
            .OrderByDescending(result => result.workItem.UpdatedAtUtc)
            .ThenBy(result => result.workItem.Id)
            .Select(result => new WorkItemSummary(
                result.workItem.Id,
                result.workItem.ApplicationSystemId,
                result.Name,
                result.workItem.Type,
                result.workItem.Title,
                result.workItem.Priority,
                result.workItem.Status,
                result.workItem.AssigneeIdentifier,
                result.workItem.DueAtUtc,
                result.workItem.CreatedAtUtc,
                result.workItem.UpdatedAtUtc,
                result.workItem.DueAtUtc != null
                    && result.workItem.DueAtUtc < normalizedAsOfUtc
                    && result.workItem.Status != WorkItemStatus.Resolved
                    && result.workItem.Status != WorkItemStatus.Closed
                    && result.workItem.Status != WorkItemStatus.Cancelled))
            .Take(filter.Limit)
            .ToListAsync(cancellationToken);
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(LikeEscapeCharacter, "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
