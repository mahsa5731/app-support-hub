namespace AppSupportHub.Application.WorkItems.AssignWorkItem;

public sealed record AssignWorkItemCommand(
    Guid WorkItemId,
    string AssigneeIdentifier,
    string ActorIdentifier);
