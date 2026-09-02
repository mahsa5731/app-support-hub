using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.ChangeApplicationSystemLifecycle;
using AppSupportHub.Application.Systems.CreateApplicationSystem;
using AppSupportHub.Application.Systems.GetApplicationSystem;
using AppSupportHub.Application.Systems.Inputs;
using AppSupportHub.Application.Systems.ListApplicationSystems;
using AppSupportHub.Application.Systems.ReadModels;
using AppSupportHub.Application.Systems.UpdateApplicationSystem;
using AppSupportHub.Web.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppSupportHub.Web.Api.V1.Systems;

public static class SystemApiEndpoints
{
    public static RouteGroupBuilder MapSystemsApi(this RouteGroupBuilder api)
    {
        RouteGroupBuilder systems = api.MapGroup("/systems").WithTags("Systems");

        systems.MapGet("/", ListAsync)
            .WithName("ListSystemsV1")
            .WithSummary("List application systems")
            .WithDescription("Returns at most 100 systems using bounded optional filters.")
            .Produces<IReadOnlyList<SystemSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
        systems.MapGet("/{id:guid}", GetAsync)
            .WithName("GetSystemV1")
            .WithSummary("Get an application system")
            .WithDescription("Returns presentation-neutral system detail by identifier.")
            .Produces<SystemDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        systems.MapPost("/", CreateAsync)
            .WithName("CreateSystemV1")
            .WithSummary("Create an application system")
            .WithDescription("Creates a system through the Application workflow.")
            .Produces<CreatedSystemResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        systems.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateSystemV1")
            .WithSummary("Update application-system metadata")
            .WithDescription("Updates metadata without changing the route-owned identifier.")
            .Produces<MutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        systems.MapPost("/{id:guid}/lifecycle", ChangeLifecycleAsync)
            .WithName("ChangeSystemLifecycleV1")
            .WithSummary("Change an application-system lifecycle")
            .WithDescription("Applies an existing lifecycle transition, including retirement.")
            .Produces<MutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return systems;
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] SystemListRequest request,
        ApplicationSystemInputFactory inputFactory,
        ListApplicationSystemsHandler handler,
        CancellationToken cancellationToken)
    {
        ApplicationResult<ListApplicationSystemsQuery> parsed = inputFactory.CreateListQuery(
            request.Name,
            request.Type,
            request.Criticality,
            request.LifecycleStatus,
            request.Limit ?? 50);

        if (!parsed.IsSuccess)
        {
            return ApplicationErrorMapper.ToProblem(parsed.Error!);
        }

        ApplicationResult<IReadOnlyList<ApplicationSystemSummary>> result =
            await handler.ExecuteAsync(parsed.Value, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value.Select(SystemSummaryResponse.From))
            : ApplicationErrorMapper.ToProblem(result.Error!);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        GetApplicationSystemHandler handler,
        CancellationToken cancellationToken)
    {
        ApplicationResult<ApplicationSystemDetail> result = await handler.ExecuteAsync(
            new GetApplicationSystemQuery(id),
            cancellationToken);
        return result.IsSuccess
            ? Results.Ok(SystemDetailResponse.From(result.Value))
            : ApplicationErrorMapper.ToProblem(result.Error!);
    }

    private static async Task<IResult> CreateAsync(
        CreateSystemRequest request,
        ApplicationSystemInputFactory inputFactory,
        CreateApplicationSystemHandler handler,
        CancellationToken cancellationToken)
    {
        ApplicationResult<CreateApplicationSystemCommand> parsed =
            inputFactory.CreateCreateCommand(
                request.Name ?? string.Empty,
                request.Description ?? string.Empty,
                request.Type ?? string.Empty,
                request.Criticality ?? string.Empty,
                request.InitialLifecycleStatus ?? string.Empty,
                request.BusinessOwner ?? string.Empty,
                request.TechnicalOwner ?? string.Empty,
                request.SupportTeam ?? string.Empty,
                request.VendorName);

        if (!parsed.IsSuccess)
        {
            return ApplicationErrorMapper.ToProblem(parsed.Error!);
        }

        ApplicationResult<CreatedApplicationSystem> result = await handler.ExecuteAsync(
            parsed.Value,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ApplicationErrorMapper.ToProblem(result.Error!);
        }

        CreatedSystemResponse response = new(result.Value.Id);
        return Results.Created($"/api/v1/systems/{result.Value.Id}", response);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateSystemRequest request,
        ApplicationSystemInputFactory inputFactory,
        UpdateApplicationSystemHandler handler,
        CancellationToken cancellationToken)
    {
        ApplicationResult<UpdateApplicationSystemCommand> parsed =
            inputFactory.CreateUpdateCommand(
                id,
                request.Name ?? string.Empty,
                request.Description ?? string.Empty,
                request.Type ?? string.Empty,
                request.Criticality ?? string.Empty,
                request.BusinessOwner ?? string.Empty,
                request.TechnicalOwner ?? string.Empty,
                request.SupportTeam ?? string.Empty,
                request.VendorName);

        if (!parsed.IsSuccess)
        {
            return ApplicationErrorMapper.ToProblem(parsed.Error!);
        }

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            parsed.Value,
            cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new MutationResponse(result.Value.Changed))
            : ApplicationErrorMapper.ToProblem(result.Error!);
    }

    private static async Task<IResult> ChangeLifecycleAsync(
        Guid id,
        ChangeSystemLifecycleRequest request,
        ApplicationSystemInputFactory inputFactory,
        ChangeApplicationSystemLifecycleHandler handler,
        CancellationToken cancellationToken)
    {
        ApplicationResult<ChangeApplicationSystemLifecycleCommand> parsed =
            inputFactory.CreateLifecycleCommand(
                id,
                request.TargetLifecycleStatus ?? string.Empty,
                request.RetirementReason);

        if (!parsed.IsSuccess)
        {
            return ApplicationErrorMapper.ToProblem(parsed.Error!);
        }

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            parsed.Value,
            cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new MutationResponse(result.Value.Changed))
            : ApplicationErrorMapper.ToProblem(result.Error!);
    }
}
