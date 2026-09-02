using AppSupportHub.Application.WorkItems.ReadModels;

namespace AppSupportHub.Web.Api.V1.WorkItems;

public sealed class WorkItemListRequest
{
    public Guid? ApplicationSystemId { get; init; }

    public string? TitleSearch { get; init; }

    public string? Type { get; init; }

    public string? Priority { get; init; }

    public string? Status { get; init; }

    public string? AssigneeIdentifier { get; init; }

    public bool? OverdueOnly { get; init; }

    public int? Limit { get; init; }
}

public sealed record CreateWorkItemRequest(
    Guid ApplicationSystemId,
    string? Type,
    string? Title,
    string? Description,
    string? Priority,
    string? DueAt);

public sealed record UpdateWorkItemRequest(string? Title, string? Description);

public sealed record AssignWorkItemRequest(string? AssigneeIdentifier);

public sealed record ChangeWorkItemPriorityRequest(string? Priority);

public sealed record ChangeWorkItemDueDateRequest(string? DueAt);

public sealed record TransitionWorkItemRequest(
    string? TargetStatus,
    string? Comment,
    string? ResolutionSummary);

public sealed record WorkItemSummaryResponse(
    Guid Id,
    Guid ApplicationSystemId,
    string ApplicationSystemName,
    string Type,
    string Title,
    string Priority,
    string Status,
    string? AssigneeIdentifier,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    bool IsOverdue)
{
    public static WorkItemSummaryResponse From(WorkItemSummary workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        return new WorkItemSummaryResponse(
            workItem.Id,
            workItem.ApplicationSystemId,
            workItem.ApplicationSystemName,
            workItem.TypeName,
            workItem.Title,
            workItem.PriorityName,
            workItem.StatusName,
            workItem.AssigneeIdentifier,
            workItem.DueAtUtc,
            workItem.CreatedAtUtc,
            workItem.UpdatedAtUtc,
            workItem.IsOverdue);
    }
}

public sealed record WorkItemHistoryResponse(
    string EventType,
    string ActorIdentifier,
    DateTimeOffset OccurredAtUtc,
    string? PreviousValue,
    string? NewValue,
    string? Comment)
{
    public static WorkItemHistoryResponse From(WorkItemHistoryItem historyItem)
    {
        ArgumentNullException.ThrowIfNull(historyItem);
        return new WorkItemHistoryResponse(
            historyItem.EventTypeName,
            historyItem.ActorIdentifier,
            historyItem.OccurredAtUtc,
            historyItem.PreviousValue,
            historyItem.NewValue,
            historyItem.Comment);
    }
}

public sealed record WorkItemDetailResponse(
    Guid Id,
    Guid ApplicationSystemId,
    string ApplicationSystemName,
    string Type,
    string Title,
    string Description,
    string Priority,
    string Status,
    string? AssigneeIdentifier,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    bool IsOverdue,
    string? ResolutionSummary,
    DateTimeOffset? ResolvedAtUtc,
    IReadOnlyList<WorkItemHistoryResponse> History)
{
    public static WorkItemDetailResponse From(WorkItemDetail workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        return new WorkItemDetailResponse(
            workItem.Id,
            workItem.ApplicationSystemId,
            workItem.ApplicationSystemName,
            workItem.TypeName,
            workItem.Title,
            workItem.Description,
            workItem.PriorityName,
            workItem.StatusName,
            workItem.AssigneeIdentifier,
            workItem.DueAtUtc,
            workItem.CreatedAtUtc,
            workItem.UpdatedAtUtc,
            workItem.IsOverdue,
            workItem.ResolutionSummary,
            workItem.ResolvedAtUtc,
            workItem.History.Select(WorkItemHistoryResponse.From).ToArray());
    }
}

public sealed record CreatedWorkItemResponse(Guid Id);

public sealed record WorkItemMutationResponse(bool Changed);
