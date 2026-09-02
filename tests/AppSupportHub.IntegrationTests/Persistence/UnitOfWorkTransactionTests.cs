using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.Infrastructure.Persistence;
using AppSupportHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppSupportHub.IntegrationTests.Persistence;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class UnitOfWorkTransactionTests(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public async Task OneSaveAtomicallyPersistsWorkItemAndAllHistoryAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem applicationSystem = PostgreSqlTestData.CreateApplicationSystem();

        await using (AppSupportHubDbContext setupContext = fixture.CreateDbContext())
        {
            setupContext.Add(applicationSystem);
            await setupContext.SaveChangesAsync();
        }

        WorkItem workItem = PostgreSqlTestData.CreateWorkItem(applicationSystem.Id);
        workItem.Assign(
            "synthetic.assignee",
            "synthetic.coordinator",
            PostgreSqlTestData.CreatedAtUtc.AddMinutes(1));

        await using (AppSupportHubDbContext unitOfWork = fixture.CreateDbContext())
        {
            await new WorkItemRepository(unitOfWork).AddAsync(workItem, CancellationToken.None);
            int savedEntries = await unitOfWork.SaveChangesAsync();
            Assert.Equal(3, savedEntries);
        }

        await using AppSupportHubDbContext readContext = fixture.CreateDbContext();
        Assert.Equal(1, await readContext.Set<WorkItem>().CountAsync());
        Assert.Equal(2, await readContext.Set<WorkItemHistoryEntry>().CountAsync());
    }

    [Fact]
    public async Task ConstraintFailureRollsBackEveryPendingChangeAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem first = PostgreSqlTestData.CreateApplicationSystem("Atomic Synthetic");
        ApplicationSystem second = PostgreSqlTestData.CreateApplicationSystem("ATOMIC SYNTHETIC");

        await using (AppSupportHubDbContext unitOfWork = fixture.CreateDbContext())
        {
            var repository = new ApplicationSystemRepository(unitOfWork);
            await repository.AddAsync(first, CancellationToken.None);
            await repository.AddAsync(second, CancellationToken.None);
            await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
        }

        await using AppSupportHubDbContext readContext = fixture.CreateDbContext();
        Assert.Equal(0, await readContext.Set<ApplicationSystem>().CountAsync());
    }
}
