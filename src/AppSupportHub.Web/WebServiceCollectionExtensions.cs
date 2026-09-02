using System.Text.Json.Serialization;
using AppSupportHub.Application.Systems.ChangeApplicationSystemLifecycle;
using AppSupportHub.Application.Systems.CreateApplicationSystem;
using AppSupportHub.Application.Systems.GetApplicationSystem;
using AppSupportHub.Application.Systems.Inputs;
using AppSupportHub.Application.Systems.ListApplicationSystems;
using AppSupportHub.Application.Systems.UpdateApplicationSystem;
using AppSupportHub.Application.WorkItems.AssignWorkItem;
using AppSupportHub.Application.WorkItems.ChangeWorkItemDueDate;
using AppSupportHub.Application.WorkItems.ChangeWorkItemPriority;
using AppSupportHub.Application.WorkItems.CreateWorkItem;
using AppSupportHub.Application.WorkItems.GetWorkItem;
using AppSupportHub.Application.WorkItems.Inputs;
using AppSupportHub.Application.WorkItems.ListWorkItems;
using AppSupportHub.Application.WorkItems.TransitionWorkItemStatus;
using AppSupportHub.Application.WorkItems.UnassignWorkItem;
using AppSupportHub.Application.WorkItems.UpdateWorkItemDetails;
using AppSupportHub.Infrastructure;
using AppSupportHub.Web.DemoData;

namespace AppSupportHub.Web;

public static class WebServiceCollectionExtensions
{
    public static IServiceCollection AddWebApplication(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddRazorPages();
        services.AddHealthChecks();
        services.AddOpenApi("v1");
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(
            new JsonStringEnumConverter(allowIntegerValues: false)));
        services.AddInfrastructure(connectionString);
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<ApplicationSystemInputFactory>();
        services.AddScoped<CreateApplicationSystemHandler>();
        services.AddScoped<GetApplicationSystemHandler>();
        services.AddScoped<ListApplicationSystemsHandler>();
        services.AddScoped<UpdateApplicationSystemHandler>();
        services.AddScoped<ChangeApplicationSystemLifecycleHandler>();

        services.AddScoped<WorkItemInputFactory>();
        services.AddScoped<CreateWorkItemHandler>();
        services.AddScoped<GetWorkItemHandler>();
        services.AddScoped<ListWorkItemsHandler>();
        services.AddScoped<UpdateWorkItemDetailsHandler>();
        services.AddScoped<AssignWorkItemHandler>();
        services.AddScoped<UnassignWorkItemHandler>();
        services.AddScoped<ChangeWorkItemPriorityHandler>();
        services.AddScoped<ChangeWorkItemDueDateHandler>();
        services.AddScoped<TransitionWorkItemStatusHandler>();

        services.AddScoped<DemoDataSeeder>();

        return services;
    }
}
