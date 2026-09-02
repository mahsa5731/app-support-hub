using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.Application.WorkItems.TransitionWorkItemStatus;

public sealed record TransitionWorkItemStatusCommand(
    Guid WorkItemId,
    WorkItemStatus TargetStatus,
    string ActorIdentifier,
    string? Comment,
    string? ResolutionSummary);
