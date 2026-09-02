using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.Application.WorkItems.ReadModels;

public sealed record WorkItemSummary(
    Guid Id,
    Guid ApplicationSystemId,
    string ApplicationSystemName,
    WorkItemType Type,
    string Title,
    WorkItemPriority Priority,
    WorkItemStatus Status,
    string? AssigneeIdentifier,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    bool IsOverdue)
{
    public string TypeName => Type.ToString();

    public string PriorityName => Priority.ToString();

    public string StatusName => Status.ToString();
}
