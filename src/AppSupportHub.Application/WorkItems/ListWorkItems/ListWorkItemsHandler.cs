using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.WorkItems.Queries;
using AppSupportHub.Application.WorkItems.ReadModels;

namespace AppSupportHub.Application.WorkItems.ListWorkItems;

public sealed class ListWorkItemsHandler
{
    private readonly IWorkItemQueries _workItemQueries;
    private readonly TimeProvider _timeProvider;

    public ListWorkItemsHandler(IWorkItemQueries workItemQueries, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(workItemQueries);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _workItemQueries = workItemQueries;
        _timeProvider = timeProvider;
    }

    public async Task<ApplicationResult<IReadOnlyList<WorkItemSummary>>> ExecuteAsync(
        ListWorkItemsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Limit is < 1 or > 100
            || query.ApplicationSystemId == Guid.Empty
            || query.Type is { } type && !Enum.IsDefined(type)
            || query.Priority is { } priority && !Enum.IsDefined(priority)
            || query.Status is { } status && !Enum.IsDefined(status))
        {
            return ApplicationResultFactory.Failure<IReadOnlyList<WorkItemSummary>>(
                InvalidInputError());
        }

        string? normalizedTitleSearch = NormalizeOptionalFilter(query.TitleSearch);
        string? normalizedAssigneeIdentifier = NormalizeOptionalFilter(query.AssigneeIdentifier);
        var filter = new WorkItemQueryFilter(
            query.ApplicationSystemId,
            normalizedTitleSearch,
            query.Type,
            query.Priority,
            query.Status,
            normalizedAssigneeIdentifier,
            query.OverdueOnly,
            query.Limit,
            _timeProvider.GetUtcNow());
        IReadOnlyList<WorkItemSummary> workItems =
            await _workItemQueries.ListAsync(filter, cancellationToken);

        return ApplicationResultFactory.Success(workItems);
    }

    private static string? NormalizeOptionalFilter(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ApplicationError InvalidInputError()
    {
        return new ApplicationError(
            "validation.invalid_input",
            "The query contains an invalid filter or limit.",
            ApplicationErrorType.Validation);
    }
}
