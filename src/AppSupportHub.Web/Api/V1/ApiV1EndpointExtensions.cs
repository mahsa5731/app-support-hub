using AppSupportHub.Web.Api.V1.Systems;
using AppSupportHub.Web.Api.V1.WorkItems;

namespace AppSupportHub.Web.Api.V1;

public static class ApiV1EndpointExtensions
{
    public static IEndpointRouteBuilder MapApiV1(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        RouteGroupBuilder api = endpoints.MapGroup("/api/v1")
            .WithTags("AppSupportHub API v1");

        api.MapSystemsApi();
        api.MapWorkItemsApi();
        return endpoints;
    }
}
