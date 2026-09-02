using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Domain.WorkItems;
using Microsoft.EntityFrameworkCore;

namespace AppSupportHub.Infrastructure.Persistence.Repositories;

public sealed class WorkItemRepository : IWorkItemRepository
{
    private readonly AppSupportHubDbContext _dbContext;

    public WorkItemRepository(AppSupportHubDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<WorkItem>()
            .Include(workItem => workItem.History.OrderBy(
                historyEntry => EF.Property<int>(
                    historyEntry,
                    AppSupportHubDbContext.HistorySequencePropertyName)))
            .SingleOrDefaultAsync(workItem => workItem.Id == id, cancellationToken);
    }

    public async Task AddAsync(WorkItem workItem, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        await _dbContext.Set<WorkItem>().AddAsync(workItem, cancellationToken);
    }
}
