using AppSupportHub.Application.WorkItems.ReadModels;

namespace AppSupportHub.Application.WorkItems.Queries;

public interface IWorkItemQueries
{
    Task<WorkItemDetail?> GetByIdAsync(
        Guid id,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkItemSummary>> ListAsync(
        WorkItemQueryFilter filter,
        CancellationToken cancellationToken);
}
