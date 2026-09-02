using System.Globalization;

namespace AppSupportHub.Domain.WorkItems;

public sealed class WorkItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 4000;
    public const int AssigneeIdentifierMaxLength = 200;
    public const int ActorIdentifierMaxLength = 200;
    public const int HistoryCommentMaxLength = 2000;
    public const int HistoryValueMaxLength = 500;
    public const int ResolutionSummaryMaxLength = 2000;

    private readonly List<WorkItemHistoryEntry> _history = [];

    private WorkItem(
        Guid id,
        Guid applicationSystemId,
        WorkItemType type,
        string title,
        string description,
        WorkItemPriority priority,
        DateTimeOffset? dueAtUtc,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        ApplicationSystemId = applicationSystemId;
        Type = type;
        Title = title;
        Description = description;
        Priority = priority;
        Status = WorkItemStatus.New;
        DueAtUtc = dueAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        History = _history.AsReadOnly();
    }

    public Guid Id { get; }

    public Guid ApplicationSystemId { get; }

    public WorkItemType Type { get; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public WorkItemPriority Priority { get; private set; }

    public WorkItemStatus Status { get; private set; }

    public string? AssigneeIdentifier { get; private set; }

    public DateTimeOffset? DueAtUtc { get; private set; }

    public string? ResolutionSummary { get; private set; }

    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<WorkItemHistoryEntry> History { get; }

    public static WorkItem Create(
        Guid applicationSystemId,
        WorkItemType type,
        string title,
        string description,
        WorkItemPriority priority,
        DateTimeOffset? dueAt,
        string actorIdentifier,
        DateTimeOffset createdAt)
    {
        if (applicationSystemId == Guid.Empty)
        {
            throw new ArgumentException(
                "Application-system ID cannot be empty.",
                nameof(applicationSystemId));
        }

        ValidateEnum(type, nameof(type));
        ValidateEnum(priority, nameof(priority));

        string normalizedTitle = NormalizeRequired(title, TitleMaxLength, nameof(title));
        string normalizedDescription = NormalizeRequired(
            description,
            DescriptionMaxLength,
            nameof(description));
        string normalizedActorIdentifier = NormalizeRequired(
            actorIdentifier,
            ActorIdentifierMaxLength,
            nameof(actorIdentifier));
        DateTimeOffset normalizedCreatedAt = createdAt.ToUniversalTime();
        DateTimeOffset? normalizedDueAt = NormalizeDueAt(dueAt, normalizedCreatedAt, nameof(dueAt));

        var workItem = new WorkItem(
            Guid.NewGuid(),
            applicationSystemId,
            type,
            normalizedTitle,
            normalizedDescription,
            priority,
            normalizedDueAt,
            normalizedCreatedAt);

        workItem.AppendHistory(
            WorkItemHistoryEventType.Created,
            normalizedActorIdentifier,
            normalizedCreatedAt,
            null,
            null,
            null);

        return workItem;
    }

    public bool UpdateDetails(
        string title,
        string description,
        string actorIdentifier,
        DateTimeOffset updatedAt)
    {
        string normalizedTitle = NormalizeRequired(title, TitleMaxLength, nameof(title));
        string normalizedDescription = NormalizeRequired(
            description,
            DescriptionMaxLength,
            nameof(description));
        string normalizedActorIdentifier = NormalizeRequired(
            actorIdentifier,
            ActorIdentifierMaxLength,
            nameof(actorIdentifier));

        bool titleChanged = normalizedTitle != Title;
        bool descriptionChanged = normalizedDescription != Description;

        if (!titleChanged && !descriptionChanged)
        {
            return false;
        }

        string? previousTitle = titleChanged ? Title : null;
        string? newTitle = titleChanged ? normalizedTitle : null;
        string changeComment = (titleChanged, descriptionChanged) switch
        {
            (true, true) => "Title and description changed.",
            (true, false) => "Title changed.",
            (false, true) => "Description changed.",
            _ => throw new InvalidOperationException("At least one detail must change."),
        };
        DateTimeOffset normalizedTimestamp = updatedAt.ToUniversalTime();

        Title = normalizedTitle;
        Description = normalizedDescription;
        UpdatedAtUtc = normalizedTimestamp;
        AppendHistory(
            WorkItemHistoryEventType.DetailsUpdated,
            normalizedActorIdentifier,
            normalizedTimestamp,
            previousTitle,
            newTitle,
            changeComment);

        return true;
    }

    public bool Assign(
        string assigneeIdentifier,
        string actorIdentifier,
        DateTimeOffset assignedAt)
    {
        string normalizedAssigneeIdentifier = NormalizeRequired(
            assigneeIdentifier,
            AssigneeIdentifierMaxLength,
            nameof(assigneeIdentifier));
        string normalizedActorIdentifier = NormalizeRequired(
            actorIdentifier,
            ActorIdentifierMaxLength,
            nameof(actorIdentifier));

        EnsureNotTerminal("Assigning a work item");

        if (normalizedAssigneeIdentifier == AssigneeIdentifier)
        {
            return false;
        }

        string? previousAssignee = AssigneeIdentifier;
        DateTimeOffset normalizedTimestamp = assignedAt.ToUniversalTime();
        AssigneeIdentifier = normalizedAssigneeIdentifier;
        UpdatedAtUtc = normalizedTimestamp;
        AppendHistory(
            WorkItemHistoryEventType.Assigned,
            normalizedActorIdentifier,
            normalizedTimestamp,
            previousAssignee,
            normalizedAssigneeIdentifier,
            null);

        return true;
    }

    public bool Unassign(string actorIdentifier, DateTimeOffset unassignedAt)
    {
        string normalizedActorIdentifier = NormalizeRequired(
            actorIdentifier,
            ActorIdentifierMaxLength,
            nameof(actorIdentifier));

        EnsureNotTerminal("Unassigning a work item");

        if (AssigneeIdentifier is null)
        {
            return false;
        }

        string previousAssignee = AssigneeIdentifier;
        DateTimeOffset normalizedTimestamp = unassignedAt.ToUniversalTime();
        AssigneeIdentifier = null;
        UpdatedAtUtc = normalizedTimestamp;
        AppendHistory(
            WorkItemHistoryEventType.Unassigned,
            normalizedActorIdentifier,
            normalizedTimestamp,
            previousAssignee,
            null,
            null);

        return true;
    }

    public bool ChangePriority(
        WorkItemPriority priority,
        string actorIdentifier,
        DateTimeOffset changedAt)
    {
        ValidateEnum(priority, nameof(priority));
        string normalizedActorIdentifier = NormalizeRequired(
            actorIdentifier,
            ActorIdentifierMaxLength,
            nameof(actorIdentifier));

        EnsureNotTerminal("Changing priority");

        if (priority == Priority)
        {
            return false;
        }

        WorkItemPriority previousPriority = Priority;
        DateTimeOffset normalizedTimestamp = changedAt.ToUniversalTime();
        Priority = priority;
        UpdatedAtUtc = normalizedTimestamp;
        AppendHistory(
            WorkItemHistoryEventType.PriorityChanged,
            normalizedActorIdentifier,
            normalizedTimestamp,
            previousPriority.ToString(),
            priority.ToString(),
            null);

        return true;
    }

    public bool ChangeDueDate(
        DateTimeOffset? dueAt,
        string actorIdentifier,
        DateTimeOffset changedAt)
    {
        string normalizedActorIdentifier = NormalizeRequired(
            actorIdentifier,
            ActorIdentifierMaxLength,
            nameof(actorIdentifier));

        EnsureNotTerminal("Changing a due date");

        DateTimeOffset? normalizedDueAt = NormalizeDueAt(dueAt, CreatedAtUtc, nameof(dueAt));

        if (normalizedDueAt == DueAtUtc)
        {
            return false;
        }

        DateTimeOffset? previousDueAt = DueAtUtc;
        DateTimeOffset normalizedTimestamp = changedAt.ToUniversalTime();
        DueAtUtc = normalizedDueAt;
        UpdatedAtUtc = normalizedTimestamp;
        AppendHistory(
            WorkItemHistoryEventType.DueDateChanged,
            normalizedActorIdentifier,
            normalizedTimestamp,
            FormatInstant(previousDueAt),
            FormatInstant(normalizedDueAt),
            null);

        return true;
    }

    public bool TransitionTo(
        WorkItemStatus targetStatus,
        string actorIdentifier,
        DateTimeOffset transitionedAt,
        string? comment = null,
        string? resolutionSummary = null)
    {
        ValidateEnum(targetStatus, nameof(targetStatus));
        string normalizedActorIdentifier = NormalizeRequired(
            actorIdentifier,
            ActorIdentifierMaxLength,
            nameof(actorIdentifier));

        if (targetStatus == Status)
        {
            return false;
        }

        if (!CanTransitionTo(targetStatus))
        {
            throw new InvalidOperationException(
                $"Work item cannot transition from {Status} to {targetStatus}.");
        }

        string? normalizedComment = NormalizeOptional(
            comment,
            HistoryCommentMaxLength,
            nameof(comment));
        string? normalizedResolutionSummary = null;

        if (targetStatus == WorkItemStatus.Resolved)
        {
            normalizedResolutionSummary = NormalizeRequired(
                resolutionSummary,
                ResolutionSummaryMaxLength,
                nameof(resolutionSummary));
        }
        else if (resolutionSummary is not null)
        {
            throw new ArgumentException(
                "A resolution summary is accepted only when resolving a work item.",
                nameof(resolutionSummary));
        }

        if (targetStatus == WorkItemStatus.Cancelled && normalizedComment is null)
        {
            throw new ArgumentException(
                "Cancelling a work item requires a comment.",
                nameof(comment));
        }

        WorkItemStatus previousStatus = Status;
        DateTimeOffset normalizedTimestamp = transitionedAt.ToUniversalTime();
        bool isReopen = previousStatus == WorkItemStatus.Resolved
            && targetStatus == WorkItemStatus.InProgress;

        Status = targetStatus;
        UpdatedAtUtc = normalizedTimestamp;

        if (targetStatus == WorkItemStatus.Resolved)
        {
            ResolutionSummary = normalizedResolutionSummary;
            ResolvedAtUtc = normalizedTimestamp;
        }
        else if (isReopen)
        {
            ResolutionSummary = null;
            ResolvedAtUtc = null;
        }

        string? statusComment = targetStatus is WorkItemStatus.Cancelled or WorkItemStatus.Resolved
            ? null
            : normalizedComment;
        AppendHistory(
            WorkItemHistoryEventType.StatusChanged,
            normalizedActorIdentifier,
            normalizedTimestamp,
            previousStatus.ToString(),
            targetStatus.ToString(),
            statusComment);

        if (targetStatus == WorkItemStatus.Resolved)
        {
            AppendHistory(
                WorkItemHistoryEventType.ResolutionRecorded,
                normalizedActorIdentifier,
                normalizedTimestamp,
                null,
                null,
                normalizedResolutionSummary);
        }
        else if (isReopen)
        {
            AppendHistory(
                WorkItemHistoryEventType.Reopened,
                normalizedActorIdentifier,
                normalizedTimestamp,
                null,
                null,
                normalizedComment);
        }
        else if (targetStatus == WorkItemStatus.Cancelled)
        {
            AppendHistory(
                WorkItemHistoryEventType.Cancelled,
                normalizedActorIdentifier,
                normalizedTimestamp,
                null,
                null,
                normalizedComment);
        }

        return true;
    }

    public bool CanTransitionTo(WorkItemStatus targetStatus)
    {
        if (!Enum.IsDefined(targetStatus))
        {
            return false;
        }

        if (targetStatus == Status)
        {
            return true;
        }

        return Status switch
        {
            WorkItemStatus.New => targetStatus is
                WorkItemStatus.UnderAnalysis or WorkItemStatus.Cancelled,
            WorkItemStatus.UnderAnalysis => targetStatus is
                WorkItemStatus.InProgress or WorkItemStatus.Blocked or WorkItemStatus.Cancelled,
            WorkItemStatus.InProgress => CanTransitionFromInProgress(targetStatus),
            WorkItemStatus.Blocked => targetStatus is
                WorkItemStatus.UnderAnalysis or WorkItemStatus.InProgress or WorkItemStatus.Cancelled,
            WorkItemStatus.Testing => targetStatus is
                WorkItemStatus.InProgress or WorkItemStatus.Resolved,
            WorkItemStatus.Resolved => targetStatus is
                WorkItemStatus.InProgress or WorkItemStatus.Closed,
            WorkItemStatus.Closed => false,
            WorkItemStatus.Cancelled => false,
            _ => false,
        };
    }

    public bool IsOverdue(DateTimeOffset now)
    {
        if (DueAtUtc is null
            || Status is WorkItemStatus.Resolved or WorkItemStatus.Closed or WorkItemStatus.Cancelled)
        {
            return false;
        }

        return now.ToUniversalTime() > DueAtUtc.Value;
    }

    private bool CanTransitionFromInProgress(WorkItemStatus targetStatus)
    {
        if (targetStatus is WorkItemStatus.Blocked or WorkItemStatus.Testing)
        {
            return true;
        }

        return Type == WorkItemType.Incident && targetStatus == WorkItemStatus.Resolved;
    }

    private void EnsureNotTerminal(string operationName)
    {
        if (Status is WorkItemStatus.Closed or WorkItemStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"{operationName} is forbidden when a work item is {Status}.");
        }
    }

    private void AppendHistory(
        WorkItemHistoryEventType eventType,
        string actorIdentifier,
        DateTimeOffset occurredAt,
        string? previousValue,
        string? newValue,
        string? comment)
    {
        _history.Add(new WorkItemHistoryEntry(
            Id,
            eventType,
            actorIdentifier,
            occurredAt,
            previousValue,
            newValue,
            comment));
    }

    private static DateTimeOffset? NormalizeDueAt(
        DateTimeOffset? dueAt,
        DateTimeOffset createdAtUtc,
        string parameterName)
    {
        if (dueAt is null)
        {
            return null;
        }

        DateTimeOffset normalizedDueAt = dueAt.Value.ToUniversalTime();

        if (normalizedDueAt <= createdAtUtc)
        {
            throw new ArgumentException(
                "Due timestamp must be later than the creation timestamp.",
                parameterName);
        }

        return normalizedDueAt;
    }

    private static string NormalizeRequired(string? value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        string normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }

    private static string? NormalizeOptional(string? value, int maximumLength, string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        string normalizedValue = value.Trim();

        if (normalizedValue.Length == 0)
        {
            return null;
        }

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }

    private static string? FormatInstant(DateTimeOffset? value)
    {
        return value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static void ValidateEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value is not defined.");
        }
    }
}
