namespace AppSupportHub.Application.WorkItems.UpdateWorkItemDetails;

public sealed record UpdateWorkItemDetailsCommand(
    Guid WorkItemId,
    string Title,
    string Description,
    string ActorIdentifier);
