namespace AppSupportHub.Domain.WorkItems;

public enum WorkItemHistoryEventType
{
    Created,
    DetailsUpdated,
    Assigned,
    Unassigned,
    PriorityChanged,
    DueDateChanged,
    StatusChanged,
    ResolutionRecorded,
    Reopened,
    Cancelled,
}
