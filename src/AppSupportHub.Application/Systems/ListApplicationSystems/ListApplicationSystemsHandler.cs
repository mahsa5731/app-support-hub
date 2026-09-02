using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.Queries;
using AppSupportHub.Application.Systems.ReadModels;

namespace AppSupportHub.Application.Systems.ListApplicationSystems;

public sealed class ListApplicationSystemsHandler
{
    private readonly IApplicationSystemQueries _applicationSystemQueries;

    public ListApplicationSystemsHandler(IApplicationSystemQueries applicationSystemQueries)
    {
        ArgumentNullException.ThrowIfNull(applicationSystemQueries);
        _applicationSystemQueries = applicationSystemQueries;
    }

    public async Task<ApplicationResult<IReadOnlyList<ApplicationSystemSummary>>> ExecuteAsync(
        ListApplicationSystemsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Limit is < 1 or > 100
            || query.Type is { } type && !Enum.IsDefined(type)
            || query.Criticality is { } criticality && !Enum.IsDefined(criticality)
            || query.LifecycleStatus is { } lifecycleStatus && !Enum.IsDefined(lifecycleStatus))
        {
            return ApplicationResultFactory.Failure<IReadOnlyList<ApplicationSystemSummary>>(
                InvalidInputError());
        }

        string? normalizedNameSearch = string.IsNullOrWhiteSpace(query.NameSearch)
            ? null
            : query.NameSearch.Trim();
        var filter = new ApplicationSystemQueryFilter(
            normalizedNameSearch,
            query.Type,
            query.Criticality,
            query.LifecycleStatus,
            query.Limit);
        IReadOnlyList<ApplicationSystemSummary> systems =
            await _applicationSystemQueries.ListAsync(filter, cancellationToken);

        return ApplicationResultFactory.Success(systems);
    }

    private static ApplicationError InvalidInputError()
    {
        return new ApplicationError(
            "validation.invalid_input",
            "The query contains an invalid filter or limit.",
            ApplicationErrorType.Validation);
    }
}
