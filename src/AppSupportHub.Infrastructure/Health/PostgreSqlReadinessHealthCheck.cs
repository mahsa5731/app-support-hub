using AppSupportHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppSupportHub.Infrastructure.Health;

public sealed class PostgreSqlReadinessHealthCheck(AppSupportHubDbContext dbContext)
{
    public Task<bool> CheckAsync(CancellationToken cancellationToken) =>
        dbContext.Database.CanConnectAsync(cancellationToken);
}
