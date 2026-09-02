using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.Application.WorkItems.ReadModels;

public sealed record WorkItemDetail(
    Guid Id,
    Guid ApplicationSystemId,
    string ApplicationSystemName,
    WorkItemType Type,
    string Title,
    string Description,
    WorkItemPriority Priority,
    WorkItemStatus Status,
    string? AssigneeIdentifier,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    bool IsOverdue,
    string? ResolutionSummary,
    DateTimeOffset? ResolvedAtUtc,
    IReadOnlyList<WorkItemHistoryItem> History);
