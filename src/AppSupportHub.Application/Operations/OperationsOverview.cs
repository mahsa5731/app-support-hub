namespace AppSupportHub.Application.Operations;

public interface IOperationsOverviewQueries
{
    Task<OperationsOverview> GetAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken);
}

public sealed record OperationsOverview(
    int TotalApplicationSystems,
    int ActiveApplicationSystems,
    int OpenWorkItems,
    int CriticalOpenWorkItems,
    int OverdueOpenWorkItems,
    int ChangeRequestWorkItems,
    DateTimeOffset AsOfUtc,
    IReadOnlyList<OperationsOverview.OverdueWorkItem> MostOverdueOpenWorkItems)
{
    public sealed record OverdueWorkItem(
        Guid Id,
        string Title,
        string ApplicationSystemName,
        string Priority,
        string Status,
        DateTimeOffset DueAtUtc,
        int OverdueDays);
}

public sealed class GetOperationsOverviewHandler(
    IOperationsOverviewQueries queries,
    TimeProvider timeProvider)
{
    public Task<OperationsOverview> ExecuteAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset asOfUtc = timeProvider.GetUtcNow().ToUniversalTime();
        return queries.GetAsync(asOfUtc, cancellationToken);
    }
}
