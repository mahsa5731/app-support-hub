using AppSupportHub.Application.WorkItems.Queries;
using AppSupportHub.Application.WorkItems.ReadModels;

namespace AppSupportHub.UnitTests.TestDoubles;

public sealed class RecordingWorkItemQueries : IWorkItemQueries
{
    public WorkItemDetail? DetailResult { get; set; }

    public IReadOnlyList<WorkItemSummary> ListResult { get; set; } = [];

    public Guid GetByIdId { get; private set; }

    public DateTimeOffset GetByIdAsOfUtc { get; private set; }

    public CancellationToken GetByIdCancellationToken { get; private set; }

    public WorkItemQueryFilter? ListFilter { get; private set; }

    public CancellationToken ListCancellationToken { get; private set; }

    public Task<WorkItemDetail?> GetByIdAsync(
        Guid id,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
    {
        GetByIdId = id;
        GetByIdAsOfUtc = asOfUtc;
        GetByIdCancellationToken = cancellationToken;
        return Task.FromResult(DetailResult);
    }

    public Task<IReadOnlyList<WorkItemSummary>> ListAsync(
        WorkItemQueryFilter filter,
        CancellationToken cancellationToken)
    {
        ListFilter = filter;
        ListCancellationToken = cancellationToken;
        return Task.FromResult(ListResult);
    }
}
