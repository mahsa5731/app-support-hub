using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.Queries;
using AppSupportHub.Application.Systems.ReadModels;
using AppSupportHub.Application.Systems.UpdateApplicationSystem;
using AppSupportHub.Domain.Systems;
using AppSupportHub.Infrastructure.Persistence;
using AppSupportHub.Infrastructure.Persistence.Queries.Systems;
using AppSupportHub.Infrastructure.Persistence.Repositories;

namespace AppSupportHub.IntegrationTests.Persistence;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class ApplicationSystemQueryTests(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public async Task ListAppliesCaseInsensitiveSearchFiltersOrderingAndLimitAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem beta = PostgreSqlTestData.CreateApplicationSystem(
            "Beta Support Portal");
        ApplicationSystem alpha = PostgreSqlTestData.CreateApplicationSystem(
            "alpha support portal");
        ApplicationSystem commercial = PostgreSqlTestData.CreateApplicationSystem(
            "Commercial Support Portal",
            ApplicationSystemType.Commercial,
            ApplicationLifecycleStatus.Active,
            "Synthetic Vendor");
        ApplicationSystem planned = PostgreSqlTestData.CreateApplicationSystem(
            "Planned Support Portal",
            lifecycleStatus: ApplicationLifecycleStatus.Planned);

        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        dbContext.AddRange(beta, alpha, commercial, planned);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var queries = new ApplicationSystemQueries(dbContext);

        IReadOnlyList<ApplicationSystemSummary> results = await queries.ListAsync(
            new ApplicationSystemQueryFilter(
                "SUPPORT PORTAL",
                ApplicationSystemType.Custom,
                ApplicationCriticality.High,
                ApplicationLifecycleStatus.Active,
                1),
            CancellationToken.None);

        ApplicationSystemSummary result = Assert.Single(results);
        Assert.Equal(alpha.Id, result.Id);
        Assert.Equal("alpha support portal", result.Name);
        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task DetailProjectsRetirementAndUpdateExcludesCurrentNameAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem payroll = PostgreSqlTestData.CreateApplicationSystem("Payroll");
        ApplicationSystem finance = PostgreSqlTestData.CreateApplicationSystem("Finance");

        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        dbContext.AddRange(payroll, finance);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var queries = new ApplicationSystemQueries(dbContext);

        ApplicationSystemDetail? detail = await queries.GetByIdAsync(
            payroll.Id,
            CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal(payroll.Description, detail.Description);
        Assert.Null(detail.RetiredAtUtc);
        Assert.Empty(dbContext.ChangeTracker.Entries());

        var repository = new ApplicationSystemRepository(dbContext);
        var handler = new UpdateApplicationSystemHandler(
            repository,
            dbContext,
            TimeProvider.System);
        var command = new UpdateApplicationSystemCommand(
            payroll.Id,
            "PAYROLL",
            payroll.Description,
            payroll.Type,
            payroll.Criticality,
            payroll.BusinessOwner,
            payroll.TechnicalOwner,
            payroll.SupportTeam,
            payroll.VendorName);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            command,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Changed);
        dbContext.ChangeTracker.Clear();
        ApplicationSystemDetail? updated = await queries.GetByIdAsync(
            payroll.Id,
            CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("PAYROLL", updated.Name);
    }
}
