using AppSupportHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AppSupportHub.IntegrationTests.Persistence;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class MigrationTests(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public async Task FreshDatabaseIsMigratedToTheLatestMigrationAsync()
    {
        await fixture.ResetDatabaseAsync();
        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();

        string[] appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync())
            .ToArray();
        string[] pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync())
            .ToArray();

        Assert.Collection(
            appliedMigrations,
            migration => Assert.EndsWith(
                "_InitialPostgreSqlPersistence",
                migration,
                StringComparison.Ordinal),
            migration => Assert.EndsWith(
                "_AddChangeAssessments",
                migration,
                StringComparison.Ordinal));
        Assert.Empty(pendingMigrations);
    }

    [Fact]
    public async Task MigrationCanDowngradeToZeroAndReapplyAsync()
    {
        await fixture.ResetDatabaseAsync();
        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        IMigrator migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync(Migration.InitialDatabase);
        Assert.Empty(await dbContext.Database.GetAppliedMigrationsAsync());

        await migrator.MigrateAsync();
        string[] migrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Equal(2, migrations.Length);
        Assert.EndsWith("_AddChangeAssessments", migrations[1], StringComparison.Ordinal);
    }
}
