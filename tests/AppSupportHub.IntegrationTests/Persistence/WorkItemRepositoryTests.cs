using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.Infrastructure.Persistence;
using AppSupportHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppSupportHub.IntegrationTests.Persistence;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class WorkItemRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public async Task WorkItemRoundTripsStateHistoryAndUtcInstantsAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem applicationSystem = PostgreSqlTestData.CreateApplicationSystem();
        WorkItem expected = PostgreSqlTestData.CreateWorkItem(applicationSystem.Id);
        expected.Assign(
            "synthetic.assignee",
            "synthetic.coordinator",
            PostgreSqlTestData.CreatedAtUtc.AddMinutes(1));
        expected.TransitionTo(
            WorkItemStatus.UnderAnalysis,
            "synthetic.analyst",
            PostgreSqlTestData.CreatedAtUtc.AddMinutes(2));
        expected.TransitionTo(
            WorkItemStatus.InProgress,
            "synthetic.analyst",
            PostgreSqlTestData.CreatedAtUtc.AddMinutes(3));
        DateTimeOffset resolvedAtUtc = PostgreSqlTestData.CreatedAtUtc.AddMinutes(4);
        expected.TransitionTo(
            WorkItemStatus.Resolved,
            "synthetic.resolver",
            resolvedAtUtc,
            resolutionSummary: "Synthetic resolution completed.");

        await using (AppSupportHubDbContext writeContext = fixture.CreateDbContext())
        {
            await new ApplicationSystemRepository(writeContext).AddAsync(
                applicationSystem,
                CancellationToken.None);
            await new WorkItemRepository(writeContext).AddAsync(expected, CancellationToken.None);
            await writeContext.SaveChangesAsync();
        }

        await using AppSupportHubDbContext readContext = fixture.CreateDbContext();
        WorkItem? actual = await new WorkItemRepository(readContext).GetByIdAsync(
            expected.Id,
            CancellationToken.None);

        Assert.NotNull(actual);
        Assert.Equal(expected.ApplicationSystemId, actual.ApplicationSystemId);
        Assert.Equal(WorkItemType.Incident, actual.Type);
        Assert.Equal(WorkItemPriority.High, actual.Priority);
        Assert.Equal(WorkItemStatus.Resolved, actual.Status);
        Assert.Equal("synthetic.assignee", actual.AssigneeIdentifier);
        Assert.Equal(expected.DueAtUtc, actual.DueAtUtc);
        Assert.Equal("Synthetic resolution completed.", actual.ResolutionSummary);
        Assert.Equal(resolvedAtUtc, actual.ResolvedAtUtc);
        Assert.Equal(resolvedAtUtc, actual.UpdatedAtUtc);
        Assert.Equal(TimeSpan.Zero, actual.UpdatedAtUtc.Offset);
        Assert.Equal(
            [
                WorkItemHistoryEventType.Created,
                WorkItemHistoryEventType.Assigned,
                WorkItemHistoryEventType.StatusChanged,
                WorkItemHistoryEventType.StatusChanged,
                WorkItemHistoryEventType.StatusChanged,
                WorkItemHistoryEventType.ResolutionRecorded,
            ],
            actual.History.Select(historyEntry => historyEntry.EventType));
    }

    [Fact]
    public async Task SameTimestampHistoryEntriesPreserveDomainAppendOrderAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem applicationSystem = PostgreSqlTestData.CreateApplicationSystem();
        WorkItem workItem = PostgreSqlTestData.CreateWorkItem(applicationSystem.Id);
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
            resolutionSummary: "Synthetic same-time resolution.");

        await using (AppSupportHubDbContext writeContext = fixture.CreateDbContext())
        {
            writeContext.Add(applicationSystem);
            await new WorkItemRepository(writeContext).AddAsync(workItem, CancellationToken.None);
            await writeContext.SaveChangesAsync();
        }

        await using AppSupportHubDbContext readContext = fixture.CreateDbContext();
        WorkItem? reloaded = await new WorkItemRepository(readContext).GetByIdAsync(
            workItem.Id,
            CancellationToken.None);

        Assert.NotNull(reloaded);
        WorkItemHistoryEntry[] sameTimestampEntries = reloaded.History
            .Where(historyEntry => historyEntry.OccurredAtUtc == sharedTimestamp)
            .ToArray();
        Assert.Equal(2, sameTimestampEntries.Length);
        Assert.Equal(WorkItemHistoryEventType.StatusChanged, sameTimestampEntries[0].EventType);
        Assert.Equal(WorkItemHistoryEventType.ResolutionRecorded, sameTimestampEntries[1].EventType);
    }

    [Fact]
    public async Task ReloadedWorkItemAppendsNextSequenceWithoutRenumberingAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem applicationSystem = PostgreSqlTestData.CreateApplicationSystem();
        WorkItem workItem = PostgreSqlTestData.CreateWorkItem(applicationSystem.Id);

        await using (AppSupportHubDbContext initialContext = fixture.CreateDbContext())
        {
            initialContext.Add(applicationSystem);
            initialContext.Add(workItem);
            await initialContext.SaveChangesAsync();
        }

        await using (AppSupportHubDbContext appendContext = fixture.CreateDbContext())
        {
            WorkItem? reloaded = await new WorkItemRepository(appendContext).GetByIdAsync(
                workItem.Id,
                CancellationToken.None);
            Assert.NotNull(reloaded);
            reloaded.Assign(
                "synthetic.assignee",
                "synthetic.coordinator",
                PostgreSqlTestData.CreatedAtUtc.AddHours(1));
            await appendContext.SaveChangesAsync();
        }

        await using AppSupportHubDbContext readContext = fixture.CreateDbContext();
        int[] sequences = await readContext.Set<WorkItemHistoryEntry>()
            .Where(historyEntry => historyEntry.WorkItemId == workItem.Id)
            .OrderBy(historyEntry => EF.Property<int>(historyEntry, "Sequence"))
            .Select(historyEntry => EF.Property<int>(historyEntry, "Sequence"))
            .ToArrayAsync();
        Assert.Equal([1, 2], sequences);
    }

    [Theory]
    [InlineData(EntityState.Modified)]
    [InlineData(EntityState.Deleted)]
    public async Task ExistingHistoryModificationOrDeletionIsRejectedAsync(EntityState state)
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem applicationSystem = PostgreSqlTestData.CreateApplicationSystem();
        WorkItem workItem = PostgreSqlTestData.CreateWorkItem(applicationSystem.Id);

        await using (AppSupportHubDbContext initialContext = fixture.CreateDbContext())
        {
            initialContext.Add(applicationSystem);
            initialContext.Add(workItem);
            await initialContext.SaveChangesAsync();
        }

        await using AppSupportHubDbContext guardedContext = fixture.CreateDbContext();
        WorkItem? reloaded = await new WorkItemRepository(guardedContext).GetByIdAsync(
            workItem.Id,
            CancellationToken.None);
        Assert.NotNull(reloaded);
        WorkItemHistoryEntry historyEntry = Assert.Single(reloaded.History);
        guardedContext.Entry(historyEntry).State = state;

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => guardedContext.SaveChangesAsync());
        Assert.Contains("append-only", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddAsyncTracksAggregateWithoutSavingAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem applicationSystem = PostgreSqlTestData.CreateApplicationSystem();
        WorkItem workItem = PostgreSqlTestData.CreateWorkItem(applicationSystem.Id);

        await using (AppSupportHubDbContext setupContext = fixture.CreateDbContext())
        {
            setupContext.Add(applicationSystem);
            await setupContext.SaveChangesAsync();
        }

        await using AppSupportHubDbContext writeContext = fixture.CreateDbContext();
        await new WorkItemRepository(writeContext).AddAsync(workItem, CancellationToken.None);
        Assert.Equal(EntityState.Added, writeContext.Entry(workItem).State);

        await using AppSupportHubDbContext readContext = fixture.CreateDbContext();
        Assert.Equal(0, await readContext.Set<WorkItem>().CountAsync());
        Assert.Equal(0, await readContext.Set<WorkItemHistoryEntry>().CountAsync());
    }

    [Fact]
    public async Task GetByIdPropagatesAnAlreadyCancelledTokenAsync()
    {
        await fixture.ResetDatabaseAsync();
        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        var repository = new WorkItemRepository(dbContext);
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetByIdAsync(Guid.NewGuid(), cancellationTokenSource.Token));
    }
}
