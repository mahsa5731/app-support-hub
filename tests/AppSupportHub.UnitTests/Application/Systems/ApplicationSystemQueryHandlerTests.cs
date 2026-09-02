using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.GetApplicationSystem;
using AppSupportHub.Application.Systems.ListApplicationSystems;
using AppSupportHub.Application.Systems.ReadModels;
using AppSupportHub.Domain.Systems;
using AppSupportHub.UnitTests.TestDoubles;

namespace AppSupportHub.UnitTests.Application.Systems;

public sealed class ApplicationSystemQueryHandlerTests
{
    [Fact]
    public async Task ListNormalizesFiltersAndPropagatesCancellationAsync()
    {
        var queries = new RecordingApplicationSystemQueries();
        var handler = new ListApplicationSystemsHandler(queries);
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<IReadOnlyList<ApplicationSystemSummary>> result =
            await handler.ExecuteAsync(
                new ListApplicationSystemsQuery(
                    "  payroll  ",
                    ApplicationSystemType.Custom,
                    ApplicationCriticality.High,
                    ApplicationLifecycleStatus.Active,
                    25),
                cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(queries.ListFilter);
        Assert.Equal("payroll", queries.ListFilter.NameSearch);
        Assert.Equal(ApplicationSystemType.Custom, queries.ListFilter.Type);
        Assert.Equal(ApplicationCriticality.High, queries.ListFilter.Criticality);
        Assert.Equal(ApplicationLifecycleStatus.Active, queries.ListFilter.LifecycleStatus);
        Assert.Equal(25, queries.ListFilter.Limit);
        Assert.Equal(cancellationToken, queries.ListCancellationToken);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ListRejectsInvalidLimitWithoutQueryingAsync(int limit)
    {
        var queries = new RecordingApplicationSystemQueries();
        var handler = new ListApplicationSystemsHandler(queries);

        ApplicationResult<IReadOnlyList<ApplicationSystemSummary>> result =
            await handler.ExecuteAsync(
                new ListApplicationSystemsQuery(Limit: limit),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error?.Code);
        Assert.Null(queries.ListFilter);
    }

    [Fact]
    public async Task ListRejectsInvalidEnumWithoutQueryingAsync()
    {
        var queries = new RecordingApplicationSystemQueries();
        var handler = new ListApplicationSystemsHandler(queries);

        ApplicationResult<IReadOnlyList<ApplicationSystemSummary>> result =
            await handler.ExecuteAsync(
                new ListApplicationSystemsQuery(Type: (ApplicationSystemType)999),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorType.Validation, result.Error?.Type);
        Assert.Null(queries.ListFilter);
    }

    [Fact]
    public async Task GetReturnsNotFoundAndPropagatesCancellationAsync()
    {
        var queries = new RecordingApplicationSystemQueries();
        var handler = new GetApplicationSystemHandler(queries);
        var id = Guid.NewGuid();
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<ApplicationSystemDetail> result = await handler.ExecuteAsync(
            new GetApplicationSystemQuery(id),
            cancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("systems.not_found", result.Error?.Code);
        Assert.Equal(id, queries.GetByIdId);
        Assert.Equal(cancellationToken, queries.GetByIdCancellationToken);
    }
}
