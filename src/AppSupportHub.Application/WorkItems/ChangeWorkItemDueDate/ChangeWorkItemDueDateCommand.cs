namespace AppSupportHub.Application.WorkItems.ChangeWorkItemDueDate;

public sealed record ChangeWorkItemDueDateCommand(
    Guid WorkItemId,
    DateTimeOffset? DueAt,
    string ActorIdentifier);
