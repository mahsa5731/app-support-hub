namespace AppSupportHub.Application.WorkItems.UnassignWorkItem;

public sealed record UnassignWorkItemCommand(Guid WorkItemId, string ActorIdentifier);
