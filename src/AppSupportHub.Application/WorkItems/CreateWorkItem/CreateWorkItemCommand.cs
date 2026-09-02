using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.Application.WorkItems.CreateWorkItem;

public sealed record CreateWorkItemCommand(
    Guid ApplicationSystemId,
    WorkItemType Type,
    string Title,
    string Description,
    WorkItemPriority Priority,
    DateTimeOffset? DueAt,
    string ActorIdentifier);
