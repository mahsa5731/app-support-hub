using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.WorkItems.ChangeWorkItemPriority;
using AppSupportHub.Application.WorkItems.CreateWorkItem;
using AppSupportHub.Application.WorkItems.ListWorkItems;
using AppSupportHub.Application.WorkItems.TransitionWorkItemStatus;
using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.Application.WorkItems.Inputs;

public sealed class WorkItemInputFactory
{
    public IReadOnlyList<string> Types { get; } =
        Array.AsReadOnly(["Incident", "Enhancement", "ChangeRequest"]);

    public IReadOnlyList<string> Priorities { get; } =
        Array.AsReadOnly(["Low", "Medium", "High", "Critical"]);

    public IReadOnlyList<string> Statuses { get; } = Array.AsReadOnly(
        ["New", "UnderAnalysis", "InProgress", "Blocked", "Testing", "Resolved", "Closed", "Cancelled"]);

    public ApplicationResult<ListWorkItemsQuery> CreateListQuery(
        Guid? applicationSystemId,
        string? titleSearch,
        string? type,
        string? priority,
        string? status,
        string? assigneeIdentifier,
        bool overdueOnly,
        int limit)
    {
        if (!TryParseOptional(type, Types, out WorkItemType? parsedType)
            || !TryParseOptional(priority, Priorities, out WorkItemPriority? parsedPriority)
            || !TryParseOptional(status, Statuses, out WorkItemStatus? parsedStatus))
        {
            return Invalid<ListWorkItemsQuery>();
        }

        return ApplicationResultFactory.Success(new ListWorkItemsQuery(
            applicationSystemId,
            titleSearch,
            parsedType,
            parsedPriority,
            parsedStatus,
            assigneeIdentifier,
            overdueOnly,
            limit));
    }

    public ApplicationResult<CreateWorkItemCommand> CreateCreateCommand(
        Guid applicationSystemId,
        string type,
        string title,
        string description,
        string priority,
        DateTimeOffset? dueAt,
        string actorIdentifier)
    {
        if (!TryParseName(type, Types, out WorkItemType parsedType)
            || !TryParseName(priority, Priorities, out WorkItemPriority parsedPriority))
        {
            return Invalid<CreateWorkItemCommand>();
        }

        return ApplicationResultFactory.Success(new CreateWorkItemCommand(
            applicationSystemId,
            parsedType,
            title,
            description,
            parsedPriority,
            dueAt,
            actorIdentifier));
    }

    public ApplicationResult<ChangeWorkItemPriorityCommand> CreatePriorityCommand(
        Guid workItemId,
        string priority,
        string actorIdentifier)
    {
        if (!TryParseName(priority, Priorities, out WorkItemPriority parsedPriority))
        {
            return Invalid<ChangeWorkItemPriorityCommand>();
        }

        return ApplicationResultFactory.Success(new ChangeWorkItemPriorityCommand(
            workItemId,
            parsedPriority,
            actorIdentifier));
    }

    public ApplicationResult<TransitionWorkItemStatusCommand> CreateTransitionCommand(
        Guid workItemId,
        string targetStatus,
        string actorIdentifier,
        string? comment,
        string? resolutionSummary)
    {
        if (!TryParseName(targetStatus, Statuses, out WorkItemStatus parsedStatus))
        {
            return Invalid<TransitionWorkItemStatusCommand>();
        }

        return ApplicationResultFactory.Success(new TransitionWorkItemStatusCommand(
            workItemId,
            parsedStatus,
            actorIdentifier,
            comment,
            resolutionSummary));
    }

    private static bool TryParseOptional<TEnum>(
        string? value,
        IReadOnlyList<string> allowedNames,
        out TEnum? parsedValue)
        where TEnum : struct, Enum
    {
        parsedValue = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!TryParseName(value, allowedNames, out TEnum parsed))
        {
            return false;
        }

        parsedValue = parsed;
        return true;
    }

    private static bool TryParseName<TEnum>(
        string? value,
        IReadOnlyList<string> allowedNames,
        out TEnum parsedValue)
        where TEnum : struct, Enum
    {
        parsedValue = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalizedValue = value.Trim();

        if (!allowedNames.Any(name => string.Equals(
            name,
            normalizedValue,
            StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return Enum.TryParse(normalizedValue, true, out parsedValue);
    }

    private static ApplicationResult<T> Invalid<T>()
    {
        return ApplicationResultFactory.Failure<T>(new ApplicationError(
            "validation.invalid_input",
            "One or more selection values are invalid.",
            ApplicationErrorType.Validation));
    }
}
