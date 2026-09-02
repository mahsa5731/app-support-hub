using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.Application.WorkItems.ChangeWorkItemPriority;

public sealed record ChangeWorkItemPriorityCommand(
    Guid WorkItemId,
    WorkItemPriority Priority,
    string ActorIdentifier);
