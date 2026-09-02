using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.Application.WorkItems.ListWorkItems;

public sealed record ListWorkItemsQuery(
    Guid? ApplicationSystemId = null,
    string? TitleSearch = null,
    WorkItemType? Type = null,
    WorkItemPriority? Priority = null,
    WorkItemStatus? Status = null,
    string? AssigneeIdentifier = null,
    bool OverdueOnly = false,
    int Limit = 50);
