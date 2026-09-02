using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.UnitTests.TestDoubles;

public sealed class InMemoryWorkItemRepository : IWorkItemRepository
{
    private readonly Dictionary<Guid, WorkItem> _workItems = [];

    public int GetByIdCallCount { get; private set; }

    public int AddCallCount { get; private set; }

    public CancellationToken GetByIdCancellationToken { get; private set; }

    public CancellationToken AddCancellationToken { get; private set; }

    public IReadOnlyCollection<WorkItem> Items => _workItems.Values;

    public Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        GetByIdCallCount++;
        GetByIdCancellationToken = cancellationToken;
        _workItems.TryGetValue(id, out WorkItem? workItem);
        return Task.FromResult(workItem);
    }

    public Task AddAsync(WorkItem workItem, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        AddCallCount++;
        AddCancellationToken = cancellationToken;
        _workItems.Add(workItem.Id, workItem);
        return Task.CompletedTask;
    }

    public void Seed(WorkItem workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        _workItems.Add(workItem.Id, workItem);
    }
}
