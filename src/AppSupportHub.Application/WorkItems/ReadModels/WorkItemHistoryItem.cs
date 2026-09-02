using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.Application.WorkItems.ReadModels;

public sealed record WorkItemHistoryItem(
    WorkItemHistoryEventType EventType,
    string ActorIdentifier,
    DateTimeOffset OccurredAtUtc,
    string? PreviousValue,
    string? NewValue,
    string? Comment);
