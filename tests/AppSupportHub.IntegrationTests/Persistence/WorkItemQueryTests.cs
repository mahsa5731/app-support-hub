using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.WorkItems.ChangeWorkItemPriority;
using AppSupportHub.Application.WorkItems.Queries;
using AppSupportHub.Application.WorkItems.ReadModels;
using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.Infrastructure.Persistence;
using AppSupportHub.Infrastructure.Persistence.Queries.WorkItems;
using AppSupportHub.Infrastructure.Persistence.Repositories;

namespace AppSupportHub.IntegrationTests.Persistence;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class WorkItemQueryTests(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public async Task ListAppliesCombinedFiltersDeterministicOrderingLimitAndOverdueAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem system = PostgreSqlTestData.CreateApplicationSystem("Support Portal");
        ApplicationSystem otherSystem = PostgreSqlTestData.CreateApplicationSystem("Other Portal");
        DateTimeOffset sharedUpdateTime = PostgreSqlTestData.CreatedAtUtc.AddHours(2);
        WorkItem first = CreateMatchingWorkItem(system.Id, "Portal outage alpha", sharedUpdateTime);
        WorkItem second = CreateMatchingWorkItem(system.Id, "Portal outage beta", sharedUpdateTime);
        WorkItem other = CreateMatchingWorkItem(
            otherSystem.Id,
            "Portal outage other",
            sharedUpdateTime);
        Guid expectedId = new[] { first.Id, second.Id }.Min();

        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        dbContext.AddRange(system, otherSystem, first, second, other);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var queries = new WorkItemQueries(dbContext);

        IReadOnlyList<WorkItemSummary> results = await queries.ListAsync(
            new WorkItemQueryFilter(
                system.Id,
                "OUTAGE",
                WorkItemType.Incident,
                WorkItemPriority.High,
                WorkItemStatus.UnderAnalysis,
                "ANALYST",
                true,
                1,
                PostgreSqlTestData.CreatedAtUtc.AddDays(10)),
            CancellationToken.None);

        WorkItemSummary result = Assert.Single(results);
        Assert.Equal(expectedId, result.Id);
        Assert.Equal(system.Id, result.ApplicationSystemId);
        Assert.Equal(system.Name, result.ApplicationSystemName);
        Assert.True(result.IsOverdue);
        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task DetailJoinsSystemAndOrdersSameTimestampHistoryBySequenceAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem system = PostgreSqlTestData.CreateApplicationSystem("Support Portal");
        WorkItem workItem = PostgreSqlTestData.CreateWorkItem(system.Id);
        workItem.TransitionTo(
            WorkItemStatus.UnderAnalysis,
            "synthetic.analyst",
            PostgreSqlTestData.CreatedAtUtc.AddMinutes(1));
        workItem.TransitionTo(
            WorkItemStatus.InProgress,
            "synthetic.analyst",
            PostgreSqlTestData.CreatedAtUtc.AddMinutes(2));
        DateTimeOffset sharedTimestamp = PostgreSqlTestData.CreatedAtUtc.AddMinutes(3);
        workItem.TransitionTo(
            WorkItemStatus.Resolved,
            "synthetic.resolver",
            sharedTimestamp,
            resolutionSummary: "Synthetic resolution.");

        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        dbContext.AddRange(system, workItem);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var queries = new WorkItemQueries(dbContext);

        WorkItemDetail? detail = await queries.GetByIdAsync(
            workItem.Id,
            sharedTimestamp.AddDays(1),
            CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal(system.Name, detail.ApplicationSystemName);
        Assert.Equal(workItem.Description, detail.Description);
        Assert.Equal("Synthetic resolution.", detail.ResolutionSummary);
        WorkItemHistoryItem[] sameTimestampEntries = detail.History
            .Where(history => history.OccurredAtUtc == sharedTimestamp)
            .ToArray();
        Assert.Equal(2, sameTimestampEntries.Length);
        Assert.Equal(WorkItemHistoryEventType.StatusChanged, sameTimestampEntries[0].EventType);
        Assert.Equal(WorkItemHistoryEventType.ResolutionRecorded, sameTimestampEntries[1].EventType);
        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task PriorityHandlerPersistsThroughRealRepositoryAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem system = PostgreSqlTestData.CreateApplicationSystem();
        WorkItem workItem = PostgreSqlTestData.CreateWorkItem(system.Id);

        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        dbContext.AddRange(system, workItem);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var repository = new WorkItemRepository(dbContext);
        var handler = new ChangeWorkItemPriorityHandler(
            repository,
            dbContext,
            TimeProvider.System);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeWorkItemPriorityCommand(
                workItem.Id,
                WorkItemPriority.Critical,
                "synthetic.analyst"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Changed);
        dbContext.ChangeTracker.Clear();
        WorkItemDetail? persisted = await new WorkItemQueries(dbContext).GetByIdAsync(
            workItem.Id,
            TimeProvider.System.GetUtcNow(),
            CancellationToken.None);
        Assert.NotNull(persisted);
        Assert.Equal(WorkItemPriority.Critical, persisted.Priority);
        Assert.Equal(WorkItemHistoryEventType.PriorityChanged, persisted.History[^1].EventType);
    }

    [Fact]
    public async Task ListPropagatesAnAlreadyCancelledTokenToEfCoreAsync()
    {
        await fixture.ResetDatabaseAsync();
        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        var queries = new WorkItemQueries(dbContext);
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queries.ListAsync(
            new WorkItemQueryFilter(
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                50,
                TimeProvider.System.GetUtcNow()),
            cancellationSource.Token));
    }

    private static WorkItem CreateMatchingWorkItem(
        Guid applicationSystemId,
        string title,
        DateTimeOffset updatedAt)
    {
        var workItem = WorkItem.Create(
            applicationSystemId,
            WorkItemType.Incident,
            title,
            "Synthetic filtered query work item.",
            WorkItemPriority.High,
            PostgreSqlTestData.CreatedAtUtc.AddDays(1),
            "synthetic.creator",
            PostgreSqlTestData.CreatedAtUtc);
        workItem.Assign("analyst", "synthetic.coordinator", updatedAt.AddMinutes(-1));
        workItem.TransitionTo(
            WorkItemStatus.UnderAnalysis,
            "synthetic.analyst",
            updatedAt);
        return workItem;
    }
}
