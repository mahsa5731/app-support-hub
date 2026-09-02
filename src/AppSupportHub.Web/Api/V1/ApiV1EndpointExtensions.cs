using AppSupportHub.Web.Api.V1.Systems;
using AppSupportHub.Web.Api.V1.WorkItems;
using AppSupportHub.Web.Security;
using Microsoft.AspNetCore.Antiforgery;

namespace AppSupportHub.Web.Api.V1;

public static class ApiV1EndpointExtensions
{
    public static IEndpointRouteBuilder MapApiV1(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        RouteGroupBuilder api = endpoints.MapGroup("/api/v1")
            .WithTags("AppSupportHub API v1");

        api.MapGet("/security/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
            {
                AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
                return Results.Ok(new AntiforgeryTokenResponse(
                    tokens.RequestToken!,
                    tokens.HeaderName!));
            })
            .WithName("GetAntiforgeryTokenV1")
            .WithSummary("Get an antiforgery token")
            .WithDescription("Returns the header name and request token for authenticated unsafe API calls.")
            .Produces<AntiforgeryTokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization(SecurityPolicies.AnalystOrAdministrator);

        api.MapSystemsApi();
        api.MapWorkItemsApi();
        return endpoints;
    }
}

public sealed record AntiforgeryTokenResponse(string RequestToken, string HeaderName);
