using AppSupportHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace AppSupportHub.IntegrationTests.Persistence;

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public AppSupportHubDbContext CreateDbContext()
    {
        DbContextOptions<AppSupportHubDbContext> options =
            new DbContextOptionsBuilder<AppSupportHubDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AppSupportHubDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using AppSupportHubDbContext dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using AppSupportHubDbContext dbContext = CreateDbContext();
        await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE work_item_history_entries, work_items, application_systems CASCADE");
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
