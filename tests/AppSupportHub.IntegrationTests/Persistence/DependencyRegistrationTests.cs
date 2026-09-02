using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.CreateApplicationSystem;
using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.Infrastructure;
using AppSupportHub.Infrastructure.Persistence;
using AppSupportHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AppSupportHub.IntegrationTests.Persistence;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class DependencyRegistrationTests(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public async Task RepositoriesAndUnitOfWorkShareOneScopedDbContextAsync()
    {
        await fixture.ResetDatabaseAsync();
        var services = new ServiceCollection();
        services.AddInfrastructure(fixture.ConnectionString);
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        AppSupportHubDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<AppSupportHubDbContext>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IApplicationSystemRepository systemRepository = scope.ServiceProvider
            .GetRequiredService<IApplicationSystemRepository>();
        IWorkItemRepository workItemRepository = scope.ServiceProvider
            .GetRequiredService<IWorkItemRepository>();
        ApplicationSystem applicationSystem = PostgreSqlTestData.CreateApplicationSystem();
        WorkItem workItem = PostgreSqlTestData.CreateWorkItem(applicationSystem.Id);

        Assert.Same(dbContext, unitOfWork);
        await systemRepository.AddAsync(applicationSystem, CancellationToken.None);
        await workItemRepository.AddAsync(workItem, CancellationToken.None);
        Assert.Equal(EntityState.Added, dbContext.Entry(applicationSystem).State);
        Assert.Equal(EntityState.Added, dbContext.Entry(workItem).State);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        Assert.Equal(1, await dbContext.Set<ApplicationSystem>().CountAsync());
        Assert.Equal(1, await dbContext.Set<WorkItem>().CountAsync());
    }

    [Fact]
    public async Task ExistingApplicationHandlerPersistsThroughRealRepositoriesAsync()
    {
        await fixture.ResetDatabaseAsync();
        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        var repository = new ApplicationSystemRepository(dbContext);
        var handler = new CreateApplicationSystemHandler(
            repository,
            dbContext,
            TimeProvider.System);
        var command = new CreateApplicationSystemCommand(
            "Handler Synthetic System",
            "Synthetic handler-to-PostgreSQL integration.",
            ApplicationSystemType.Custom,
            ApplicationCriticality.Medium,
            ApplicationLifecycleStatus.Active,
            "Synthetic Business Owner",
            "Synthetic Technical Owner",
            "Synthetic Support Team",
            null);

        ApplicationResult<CreatedApplicationSystem> result = await handler.ExecuteAsync(
            command,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        dbContext.ChangeTracker.Clear();
        ApplicationSystem? persisted = await repository.GetByIdAsync(
            result.Value.Id,
            CancellationToken.None);
        Assert.NotNull(persisted);
        Assert.Equal(command.Name, persisted.Name);
    }

    [Fact]
    public void RegistrationRejectsBlankConnectionString()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddInfrastructure("  "));
    }

    [Fact]
    public void RepositoriesImplementOnlyTheirSpecificApplicationContracts()
    {
        Assert.Equal(
            [typeof(IApplicationSystemRepository)],
            typeof(ApplicationSystemRepository).GetInterfaces());
        Assert.Equal(
            [typeof(IWorkItemRepository)],
            typeof(WorkItemRepository).GetInterfaces());
    }
}
