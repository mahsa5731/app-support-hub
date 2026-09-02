using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AppSupportHub.Infrastructure.Persistence;

public sealed class AppSupportHubDbContextFactory
    : IDesignTimeDbContextFactory<AppSupportHubDbContext>
{
    private const string ConnectionStringEnvironmentVariable =
        "ConnectionStrings__AppSupportHub";
    private const string DesignTimeConnectionString =
        "Host=localhost;Database=appsupporthub_design;Username=appsupporthub";

    public AppSupportHubDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable) ?? DesignTimeConnectionString;
        var optionsBuilder = new DbContextOptionsBuilder<AppSupportHubDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppSupportHubDbContext(optionsBuilder.Options);
    }
}
