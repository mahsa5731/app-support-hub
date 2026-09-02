using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.WorkItems.AssignWorkItem;
using AppSupportHub.Application.WorkItems.ChangeWorkItemDueDate;
using AppSupportHub.Application.WorkItems.ChangeWorkItemPriority;
using AppSupportHub.Application.WorkItems.CreateWorkItem;
using AppSupportHub.Application.WorkItems.GetWorkItem;
using AppSupportHub.Application.WorkItems.Inputs;
using AppSupportHub.Application.WorkItems.ListWorkItems;
using AppSupportHub.Application.WorkItems.ReadModels;
using AppSupportHub.Application.WorkItems.TransitionWorkItemStatus;
using AppSupportHub.Application.WorkItems.UnassignWorkItem;
using AppSupportHub.Application.WorkItems.UpdateWorkItemDetails;
using AppSupportHub.Web.Http;
using AppSupportHub.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace AppSupportHub.Web.Api.V1.WorkItems;

public static class WorkItemApiEndpoints
{
    public static RouteGroupBuilder MapWorkItemsApi(this RouteGroupBuilder api)
    {
        RouteGroupBuilder workItems = api.MapGroup("/work-items").WithTags("Work items");
        RouteGroupBuilder writes = workItems.MapGroup(string.Empty)
            .RequireAuthorization(SecurityPolicies.AnalystOrAdministrator)
            .RequireRateLimiting(SecurityPolicies.UnsafeApiRateLimit)
            .RequireApiAntiforgery();

        workItems.MapGet("/", ListAsync)
            .WithName("ListWorkItemsV1")
            .WithSummary("List work items")
            .WithDescription("Returns at most 100 work items using the supported bounded filters.")
            .Produces<IReadOnlyList<WorkItemSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
        workItems.MapGet("/{id:guid}", GetAsync)
            .WithName("GetWorkItemV1")
            .WithSummary("Get a work item")
            .WithDescription("Returns work-item detail and its chronological immutable history.")
            .Produces<WorkItemDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        writes.MapPost("/", CreateAsync)
            .WithName("CreateWorkItemV1")
            .WithSummary("Create a work item")
            .WithDescription("Creates a work item for a non-retired application system.")
            .Produces<CreatedWorkItemResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        writes.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateWorkItemV1")
            .WithSummary("Update work-item details")
            .WithDescription("Updates the title and description for the route-owned work item.")
            .Produces<WorkItemMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        writes.MapPut("/{id:guid}/assignment", AssignAsync)
            .WithName("AssignWorkItemV1")
            .WithSummary("Assign a work item")
            .WithDescription("Assigns the route-owned work item using the authenticated actor.")
            .Produces<WorkItemMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        writes.MapDelete("/{id:guid}/assignment", UnassignAsync)
            .WithName("UnassignWorkItemV1")
            .WithSummary("Unassign a work item")
            .WithDescription("Removes the current assignment using the authenticated actor.")
            .Produces<WorkItemMutationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        writes.MapPut("/{id:guid}/priority", ChangePriorityAsync)
            .WithName("ChangeWorkItemPriorityV1")
            .WithSummary("Change work-item priority")
            .WithDescription("Changes priority using the authenticated actor.")
            .Produces<WorkItemMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        writes.MapPut("/{id:guid}/due-date", ChangeDueDateAsync)
            .WithName("ChangeWorkItemDueDateV1")
            .WithSummary("Change work-item due date")
            .WithDescription("Sets or clears an ISO 8601 due date with an explicit offset.")
            .Produces<WorkItemMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        writes.MapPost("/{id:guid}/transitions", TransitionAsync)
            .WithName("TransitionWorkItemV1")
            .WithSummary("Transition work-item status")
            .WithDescription("Applies a valid status transition and optional resolution data.")
            .Produces<WorkItemMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return workItems;
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] WorkItemListRequest request,
        WorkItemInputFactory inputFactory,
        ListWorkItemsHandler handler,
        CancellationToken cancellationToken)
    {
        ApplicationResult<ListWorkItemsQuery> parsed = inputFactory.CreateListQuery(
            request.ApplicationSystemId,
            request.TitleSearch,
            request.Type,
            request.Priority,
            request.Status,
            request.AssigneeIdentifier,
            request.OverdueOnly ?? false,
            request.Limit ?? 50);
        if (!parsed.IsSuccess)
        {
            return ApplicationErrorMapper.ToProblem(parsed.Error!);
        }

        ApplicationResult<IReadOnlyList<WorkItemSummary>> result =
            await handler.ExecuteAsync(parsed.Value, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value.Select(WorkItemSummaryResponse.From))
            : ApplicationErrorMapper.ToProblem(result.Error!);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        GetWorkItemHandler handler,
        CancellationToken cancellationToken)
    {
        ApplicationResult<WorkItemDetail> result = await handler.ExecuteAsync(
            new GetWorkItemQuery(id),
            cancellationToken);
        return result.IsSuccess
            ? Results.Ok(WorkItemDetailResponse.From(result.Value))
            : ApplicationErrorMapper.ToProblem(result.Error!);
    }

    private static async Task<IResult> CreateAsync(
        CreateWorkItemRequest request,
        WorkItemInputFactory inputFactory,
        CreateWorkItemHandler handler,
        CurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        if (!UtcInputParser.TryParseIso8601WithOffset(request.DueAt, out DateTimeOffset? dueAt))
        {
            return InvalidDateProblem();
        }

        ApplicationResult<CreateWorkItemCommand> parsed = inputFactory.CreateCreateCommand(
            request.ApplicationSystemId,
            request.Type ?? string.Empty,
            request.Title ?? string.Empty,
            request.Description ?? string.Empty,
            request.Priority ?? string.Empty,
            dueAt,
            currentActor.GetRequiredUsername());
        if (!parsed.IsSuccess)
        {
            return ApplicationErrorMapper.ToProblem(parsed.Error!);
        }

        ApplicationResult<CreatedWorkItem> result = await handler.ExecuteAsync(
            parsed.Value,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ApplicationErrorMapper.ToProblem(result.Error!);
        }

        return Results.Created(
            $"/api/v1/work-items/{result.Value.Id}",
            new CreatedWorkItemResponse(result.Value.Id));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateWorkItemRequest request,
        UpdateWorkItemDetailsHandler handler,
        CurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new UpdateWorkItemDetailsCommand(
                id,
                request.Title ?? string.Empty,
                request.Description ?? string.Empty,
                currentActor.GetRequiredUsername()),
            cancellationToken);
        return MutationResult(result);
    }

    private static async Task<IResult> AssignAsync(
        Guid id,
        AssignWorkItemRequest request,
        AssignWorkItemHandler handler,
        CurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new AssignWorkItemCommand(
                id,
                request.AssigneeIdentifier ?? string.Empty,
                currentActor.GetRequiredUsername()),
            cancellationToken);
        return MutationResult(result);
    }

    private static async Task<IResult> UnassignAsync(
        Guid id,
        UnassignWorkItemHandler handler,
        CurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new UnassignWorkItemCommand(id, currentActor.GetRequiredUsername()),
            cancellationToken);
        return MutationResult(result);
    }

    private static async Task<IResult> ChangePriorityAsync(
        Guid id,
        ChangeWorkItemPriorityRequest request,
        WorkItemInputFactory inputFactory,
        ChangeWorkItemPriorityHandler handler,
        CurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        ApplicationResult<ChangeWorkItemPriorityCommand> parsed =
            inputFactory.CreatePriorityCommand(
                id,
                request.Priority ?? string.Empty,
                currentActor.GetRequiredUsername());
        if (!parsed.IsSuccess)
        {
            return ApplicationErrorMapper.ToProblem(parsed.Error!);
        }

        return MutationResult(await handler.ExecuteAsync(parsed.Value, cancellationToken));
    }

    private static async Task<IResult> ChangeDueDateAsync(
        Guid id,
        ChangeWorkItemDueDateRequest request,
        ChangeWorkItemDueDateHandler handler,
        CurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        if (!UtcInputParser.TryParseIso8601WithOffset(request.DueAt, out DateTimeOffset? dueAt))
        {
            return InvalidDateProblem();
        }

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeWorkItemDueDateCommand(id, dueAt, currentActor.GetRequiredUsername()),
            cancellationToken);
        return MutationResult(result);
    }

    private static async Task<IResult> TransitionAsync(
        Guid id,
        TransitionWorkItemRequest request,
        WorkItemInputFactory inputFactory,
        TransitionWorkItemStatusHandler handler,
        CurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        ApplicationResult<TransitionWorkItemStatusCommand> parsed =
            inputFactory.CreateTransitionCommand(
                id,
                request.TargetStatus ?? string.Empty,
                currentActor.GetRequiredUsername(),
                request.Comment,
                request.ResolutionSummary);
        if (!parsed.IsSuccess)
        {
            return ApplicationErrorMapper.ToProblem(parsed.Error!);
        }

        return MutationResult(await handler.ExecuteAsync(parsed.Value, cancellationToken));
    }

    private static IResult MutationResult(ApplicationResult<MutationOutcome> result)
    {
        return result.IsSuccess
            ? Results.Ok(new WorkItemMutationResponse(result.Value.Changed))
            : ApplicationErrorMapper.ToProblem(result.Error!);
    }

    private static IResult InvalidDateProblem()
    {
        return ApplicationErrorMapper.ToProblem(new ApplicationError(
            "validation.invalid_input",
            "DueAt must be an ISO 8601 date and time with an explicit offset.",
            ApplicationErrorType.Validation));
    }
}
