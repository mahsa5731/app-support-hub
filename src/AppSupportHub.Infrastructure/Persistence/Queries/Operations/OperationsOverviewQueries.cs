using AppSupportHub.Application.Operations;
using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;
using Microsoft.EntityFrameworkCore;

namespace AppSupportHub.Infrastructure.Persistence.Queries.Operations;

public sealed class OperationsOverviewQueries(AppSupportHubDbContext dbContext)
    : IOperationsOverviewQueries
{
    public async Task<OperationsOverview> GetAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
    {
        DateTimeOffset normalizedAsOfUtc = asOfUtc.ToUniversalTime();
        SystemCounts systems = await dbContext.Set<ApplicationSystem>()
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new SystemCounts(
                group.Count(),
                group.Count(system => system.LifecycleStatus == ApplicationLifecycleStatus.Active)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new SystemCounts(0, 0);

        WorkItemCounts workItems = await dbContext.Set<WorkItem>()
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new WorkItemCounts(
                group.Count(item => item.Status != WorkItemStatus.Closed
                    && item.Status != WorkItemStatus.Cancelled),
                group.Count(item => item.Priority == WorkItemPriority.Critical
                    && item.Status != WorkItemStatus.Closed
                    && item.Status != WorkItemStatus.Cancelled),
                group.Count(item => item.DueAtUtc != null
                    && item.DueAtUtc < normalizedAsOfUtc
                    && item.Status != WorkItemStatus.Closed
                    && item.Status != WorkItemStatus.Cancelled),
                group.Count(item => item.Type == WorkItemType.ChangeRequest)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new WorkItemCounts(0, 0, 0, 0);

        List<OperationsOverview.OverdueWorkItem> overdue = await dbContext.Set<WorkItem>()
            .AsNoTracking()
            .Where(item => item.DueAtUtc != null
                && item.DueAtUtc < normalizedAsOfUtc
                && item.Status != WorkItemStatus.Closed
                && item.Status != WorkItemStatus.Cancelled)
            .Join(
                dbContext.Set<ApplicationSystem>().AsNoTracking(),
                item => item.ApplicationSystemId,
                system => system.Id,
                (item, system) => new { item, system.Name })
            .OrderBy(result => result.item.DueAtUtc)
            .ThenBy(result => result.item.Id)
            .Take(5)
            .Select(result => new OperationsOverview.OverdueWorkItem(
                result.item.Id,
                result.item.Title,
                result.Name,
                result.item.Priority.ToString(),
                result.item.Status.ToString(),
                result.item.DueAtUtc!.Value,
                (int)Math.Ceiling(
                    (normalizedAsOfUtc - result.item.DueAtUtc.Value).TotalDays)))
            .ToListAsync(cancellationToken);

        return new OperationsOverview(
            systems.Total,
            systems.Active,
            workItems.Open,
            workItems.CriticalOpen,
            workItems.OverdueOpen,
            workItems.ChangeRequests,
            normalizedAsOfUtc,
            overdue);
    }

    private sealed record SystemCounts(int Total, int Active);

    private sealed record WorkItemCounts(
        int Open,
        int CriticalOpen,
        int OverdueOpen,
        int ChangeRequests);
}
