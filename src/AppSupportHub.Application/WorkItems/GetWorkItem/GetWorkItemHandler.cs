using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.WorkItems.Queries;
using AppSupportHub.Application.WorkItems.ReadModels;

namespace AppSupportHub.Application.WorkItems.GetWorkItem;

public sealed class GetWorkItemHandler
{
    private readonly IWorkItemQueries _workItemQueries;
    private readonly TimeProvider _timeProvider;

    public GetWorkItemHandler(IWorkItemQueries workItemQueries, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(workItemQueries);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _workItemQueries = workItemQueries;
        _timeProvider = timeProvider;
    }

    public async Task<ApplicationResult<WorkItemDetail>> ExecuteAsync(
        GetWorkItemQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        WorkItemDetail? workItem = await _workItemQueries.GetByIdAsync(
            query.WorkItemId,
            _timeProvider.GetUtcNow(),
            cancellationToken);

        if (workItem is null)
        {
            return ApplicationResultFactory.Failure<WorkItemDetail>(new ApplicationError(
                "work_items.not_found",
                "The work item was not found.",
                ApplicationErrorType.NotFound));
        }

        return ApplicationResultFactory.Success(workItem);
    }
}
