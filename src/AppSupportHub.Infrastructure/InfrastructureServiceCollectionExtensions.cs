using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Application.LegacyImports;
using AppSupportHub.Application.Systems.Queries;
using AppSupportHub.Application.WorkItems.Queries;
using AppSupportHub.Infrastructure.LegacyImports;
using AppSupportHub.Infrastructure.Persistence;
using AppSupportHub.Infrastructure.Persistence.Queries.Systems;
using AppSupportHub.Infrastructure.Persistence.Queries.WorkItems;
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
        services.AddScoped<IChangeAssessmentRepository, ChangeAssessmentRepository>();
        services.AddScoped<IApplicationSystemRepository, ApplicationSystemRepository>();
        services.AddScoped<IWorkItemRepository, WorkItemRepository>();
        services.AddScoped<IApplicationSystemQueries, ApplicationSystemQueries>();
        services.AddScoped<IWorkItemQueries, WorkItemQueries>();
        services.AddScoped<ILegacyCsvParser, CsvHelperLegacyCsvParser>();
        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<AppSupportHubDbContext>());

        return services;
    }
}
