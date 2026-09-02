using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.WorkItems.GetWorkItem;
using AppSupportHub.Application.WorkItems.ListWorkItems;
using AppSupportHub.Application.WorkItems.ReadModels;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.UnitTests.TestDoubles;

namespace AppSupportHub.UnitTests.Application.WorkItems;

public sealed class WorkItemQueryHandlerTests
{
    private static readonly DateTimeOffset _currentTime =
        new(2026, 4, 8, 14, 30, 0, TimeSpan.FromHours(-5));

    [Fact]
    public async Task ListNormalizesFiltersSuppliesUtcTimeAndPropagatesCancellationAsync()
    {
        var queries = new RecordingWorkItemQueries();
        var handler = new ListWorkItemsHandler(queries, new FixedTimeProvider(_currentTime));
        var applicationSystemId = Guid.NewGuid();
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<IReadOnlyList<WorkItemSummary>> result = await handler.ExecuteAsync(
            new ListWorkItemsQuery(
                applicationSystemId,
                "  outage  ",
                WorkItemType.Incident,
                WorkItemPriority.Critical,
                WorkItemStatus.InProgress,
                "  analyst  ",
                true,
                20),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(queries.ListFilter);
        Assert.Equal(applicationSystemId, queries.ListFilter.ApplicationSystemId);
        Assert.Equal("outage", queries.ListFilter.TitleSearch);
        Assert.Equal("analyst", queries.ListFilter.AssigneeIdentifier);
        Assert.True(queries.ListFilter.OverdueOnly);
        Assert.Equal(20, queries.ListFilter.Limit);
        Assert.Equal(_currentTime.ToUniversalTime(), queries.ListFilter.AsOfUtc);
        Assert.Equal(cancellationToken, queries.ListCancellationToken);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ListRejectsInvalidLimitWithoutQueryingAsync(int limit)
    {
        var queries = new RecordingWorkItemQueries();
        var handler = new ListWorkItemsHandler(queries, new FixedTimeProvider(_currentTime));

        ApplicationResult<IReadOnlyList<WorkItemSummary>> result = await handler.ExecuteAsync(
            new ListWorkItemsQuery(Limit: limit),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error?.Code);
        Assert.Null(queries.ListFilter);
    }

    [Fact]
    public async Task ListRejectsInvalidEnumWithoutQueryingAsync()
    {
        var queries = new RecordingWorkItemQueries();
        var handler = new ListWorkItemsHandler(queries, new FixedTimeProvider(_currentTime));

        ApplicationResult<IReadOnlyList<WorkItemSummary>> result = await handler.ExecuteAsync(
            new ListWorkItemsQuery(Priority: (WorkItemPriority)999),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorType.Validation, result.Error?.Type);
        Assert.Null(queries.ListFilter);
    }

    [Fact]
    public async Task GetReturnsNotFoundWithTimeAndCancellationAsync()
    {
        var queries = new RecordingWorkItemQueries();
        var handler = new GetWorkItemHandler(queries, new FixedTimeProvider(_currentTime));
        var id = Guid.NewGuid();
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<WorkItemDetail> result = await handler.ExecuteAsync(
            new GetWorkItemQuery(id),
            cancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("work_items.not_found", result.Error?.Code);
        Assert.Equal(id, queries.GetByIdId);
        Assert.Equal(_currentTime.ToUniversalTime(), queries.GetByIdAsOfUtc);
        Assert.Equal(cancellationToken, queries.GetByIdCancellationToken);
    }
}
