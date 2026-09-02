using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Infrastructure.Persistence;
using AppSupportHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AppSupportHub.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<AppSupportHubDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IApplicationSystemRepository, ApplicationSystemRepository>();
        services.AddScoped<IWorkItemRepository, WorkItemRepository>();
        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<AppSupportHubDbContext>());

        return services;
    }
}
