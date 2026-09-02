using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.Application.WorkItems.Queries;

public sealed record WorkItemQueryFilter(
    Guid? ApplicationSystemId,
    string? TitleSearch,
    WorkItemType? Type,
    WorkItemPriority? Priority,
    WorkItemStatus? Status,
    string? AssigneeIdentifier,
    bool OverdueOnly,
    int Limit,
    DateTimeOffset AsOfUtc);
