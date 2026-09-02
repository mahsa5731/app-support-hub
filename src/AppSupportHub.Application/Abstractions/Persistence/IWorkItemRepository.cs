using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.Application.Abstractions.Persistence;

public interface IWorkItemRepository
{
    Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(WorkItem workItem, CancellationToken cancellationToken);
}
