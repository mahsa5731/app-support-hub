using AppSupportHub.Domain.Systems;
using AppSupportHub.Infrastructure.Persistence;
using AppSupportHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppSupportHub.IntegrationTests.Persistence;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class ApplicationSystemRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public async Task ApplicationSystemRoundTripsAllMetadataAndUtcInstantsAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem expected = PostgreSqlTestData.CreateApplicationSystem(
            "Synthetic Commercial Platform",
            ApplicationSystemType.Commercial,
            ApplicationLifecycleStatus.Active,
            "Synthetic Vendor");

        await using (AppSupportHubDbContext writeContext = fixture.CreateDbContext())
        {
            var repository = new ApplicationSystemRepository(writeContext);
            await repository.AddAsync(expected, CancellationToken.None);
            await writeContext.SaveChangesAsync();
        }

        await using AppSupportHubDbContext readContext = fixture.CreateDbContext();
        var readRepository = new ApplicationSystemRepository(readContext);
        ApplicationSystem? actual = await readRepository.GetByIdAsync(
            expected.Id,
            CancellationToken.None);

        Assert.NotNull(actual);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.Criticality, actual.Criticality);
        Assert.Equal(expected.LifecycleStatus, actual.LifecycleStatus);
        Assert.Equal(expected.BusinessOwner, actual.BusinessOwner);
        Assert.Equal(expected.TechnicalOwner, actual.TechnicalOwner);
        Assert.Equal(expected.SupportTeam, actual.SupportTeam);
        Assert.Equal(expected.VendorName, actual.VendorName);
        Assert.Equal(expected.CreatedAtUtc, actual.CreatedAtUtc);
        Assert.Equal(expected.UpdatedAtUtc, actual.UpdatedAtUtc);
        Assert.Equal(TimeSpan.Zero, actual.CreatedAtUtc.Offset);
        Assert.Equal(EntityState.Unchanged, readContext.Entry(actual).State);
    }

    [Fact]
    public async Task RetiredApplicationSystemRoundTripsRetirementStateAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem expected = PostgreSqlTestData.CreateApplicationSystem(
            lifecycleStatus: ApplicationLifecycleStatus.Planned);
        DateTimeOffset retiredAtUtc = PostgreSqlTestData.CreatedAtUtc.AddDays(10);
        expected.TransitionLifecycle(
            ApplicationLifecycleStatus.Retired,
            retiredAtUtc,
            "Synthetic retirement reason.");

        await using (AppSupportHubDbContext writeContext = fixture.CreateDbContext())
        {
            var repository = new ApplicationSystemRepository(writeContext);
            await repository.AddAsync(expected, CancellationToken.None);
            await writeContext.SaveChangesAsync();
        }

        await using AppSupportHubDbContext readContext = fixture.CreateDbContext();
        var readRepository = new ApplicationSystemRepository(readContext);
        ApplicationSystem? actual = await readRepository.GetByIdAsync(
            expected.Id,
            CancellationToken.None);

        Assert.NotNull(actual);
        Assert.True(actual.IsRetired);
        Assert.Equal(retiredAtUtc, actual.RetiredAtUtc);
        Assert.Equal("Synthetic retirement reason.", actual.RetirementReason);
        Assert.Equal(retiredAtUtc, actual.UpdatedAtUtc);
    }

    [Fact]
    public async Task NameLookupIsCaseInsensitiveExactAndDoesNotTreatWildcardsSpeciallyAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem applicationSystem = PostgreSqlTestData.CreateApplicationSystem(
            "Synthetic%_Portal");

        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        var repository = new ApplicationSystemRepository(dbContext);
        await repository.AddAsync(applicationSystem, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        Assert.True(await repository.NameExistsAsync(
            "  synthetic%_portal  ",
            CancellationToken.None));
        Assert.False(await repository.NameExistsAsync(
            "SyntheticValuePortal",
            CancellationToken.None));
        Assert.False(await repository.NameExistsAsync(
            "Synthetic%",
            CancellationToken.None));
    }

    [Fact]
    public async Task DatabaseRejectsDuplicateNamesThatDifferOnlyByCaseAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem first = PostgreSqlTestData.CreateApplicationSystem("Synthetic Portal");
        ApplicationSystem second = PostgreSqlTestData.CreateApplicationSystem("SYNTHETIC PORTAL");

        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        var repository = new ApplicationSystemRepository(dbContext);
        await repository.AddAsync(first, CancellationToken.None);
        await dbContext.SaveChangesAsync();
        await repository.AddAsync(second, CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task AddAsyncTracksWithoutSavingAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem applicationSystem = PostgreSqlTestData.CreateApplicationSystem();

        await using AppSupportHubDbContext writeContext = fixture.CreateDbContext();
        var repository = new ApplicationSystemRepository(writeContext);
        await repository.AddAsync(applicationSystem, CancellationToken.None);

        Assert.Equal(EntityState.Added, writeContext.Entry(applicationSystem).State);
        await using AppSupportHubDbContext readContext = fixture.CreateDbContext();
        Assert.Equal(0, await readContext.Set<ApplicationSystem>().CountAsync());
    }

    [Fact]
    public async Task GetByIdPropagatesAnAlreadyCancelledTokenAsync()
    {
        await fixture.ResetDatabaseAsync();
        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        var repository = new ApplicationSystemRepository(dbContext);
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetByIdAsync(Guid.NewGuid(), cancellationTokenSource.Token));
    }
}
