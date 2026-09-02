namespace AppSupportHub.Domain.WorkItems;

public sealed class WorkItemHistoryEntry
{
    internal WorkItemHistoryEntry(
        Guid workItemId,
        WorkItemHistoryEventType eventType,
        string actorIdentifier,
        DateTimeOffset occurredAt,
        string? previousValue,
        string? newValue,
        string? comment)
    {
        if (workItemId == Guid.Empty)
        {
            throw new ArgumentException("Work-item ID cannot be empty.", nameof(workItemId));
        }

        if (!Enum.IsDefined(eventType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(eventType),
                eventType,
                "History event type is not defined.");
        }

        Id = Guid.NewGuid();
        WorkItemId = workItemId;
        EventType = eventType;
        ActorIdentifier = NormalizeRequired(
            actorIdentifier,
            WorkItem.ActorIdentifierMaxLength,
            nameof(actorIdentifier));
        OccurredAtUtc = occurredAt.ToUniversalTime();
        PreviousValue = NormalizeOptional(
            previousValue,
            WorkItem.HistoryValueMaxLength,
            nameof(previousValue));
        NewValue = NormalizeOptional(
            newValue,
            WorkItem.HistoryValueMaxLength,
            nameof(newValue));
        Comment = NormalizeOptional(
            comment,
            WorkItem.HistoryCommentMaxLength,
            nameof(comment));
    }

    public Guid Id { get; }

    public Guid WorkItemId { get; }

    public WorkItemHistoryEventType EventType { get; }

    public string ActorIdentifier { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public string? PreviousValue { get; }

    public string? NewValue { get; }

    public string? Comment { get; }

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
}
