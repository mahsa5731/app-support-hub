using AppSupportHub.Domain.WorkItems;
using AppSupportHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AppSupportHub.IntegrationTests.Persistence;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class PersistenceConstraintTests(PostgreSqlContainerFixture fixture)
{
    [Theory]
    [InlineData("commercial_without_vendor")]
    [InlineData("retired_without_retirement_state")]
    public async Task DatabaseRejectsInvalidApplicationSystemStateAsync(string violation)
    {
        await fixture.ResetDatabaseAsync();
        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        string systemType = violation == "commercial_without_vendor" ? "Commercial" : "Custom";
        string lifecycleStatus = violation == "retired_without_retirement_state"
            ? "Retired"
            : "Active";
        FormattableString command = $"""
            INSERT INTO application_systems
                (id, name, description, "type", criticality, lifecycle_status,
                 business_owner, technical_owner, support_team, created_at_utc, updated_at_utc)
            VALUES
                ({Guid.NewGuid()}, {"Invalid synthetic system"}, {"Synthetic invalid state"},
                 {systemType}, {"High"}, {lifecycleStatus}, {"Synthetic owner"},
                 {"Synthetic technician"}, {"Synthetic team"},
                 {PostgreSqlTestData.CreatedAtUtc}, {PostgreSqlTestData.CreatedAtUtc})
            """;

        await Assert.ThrowsAsync<PostgresException>(async () =>
            await dbContext.Database.ExecuteSqlInterpolatedAsync(command));
    }

    [Fact]
    public async Task DatabaseRejectsMissingApplicationSystemForeignKeyAsync()
    {
        await fixture.ResetDatabaseAsync();
        WorkItem workItem = PostgreSqlTestData.CreateWorkItem(Guid.NewGuid());
        await using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        dbContext.Add(workItem);

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }
}
