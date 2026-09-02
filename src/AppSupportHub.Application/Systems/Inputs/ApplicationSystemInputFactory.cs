using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.ChangeApplicationSystemLifecycle;
using AppSupportHub.Application.Systems.CreateApplicationSystem;
using AppSupportHub.Application.Systems.ListApplicationSystems;
using AppSupportHub.Application.Systems.UpdateApplicationSystem;
using AppSupportHub.Domain.Systems;

namespace AppSupportHub.Application.Systems.Inputs;

public sealed class ApplicationSystemInputFactory
{
    public IReadOnlyList<string> Types { get; } =
        Array.AsReadOnly(["Commercial", "Custom"]);

    public IReadOnlyList<string> Criticalities { get; } =
        Array.AsReadOnly(["Low", "Medium", "High", "Critical"]);

    public IReadOnlyList<string> LifecycleStatuses { get; } =
        Array.AsReadOnly(["Planned", "Active", "Maintenance", "Retired"]);

    public IReadOnlyList<string> InitialLifecycleStatuses { get; } =
        Array.AsReadOnly(["Planned", "Active"]);

    public ApplicationResult<ListApplicationSystemsQuery> CreateListQuery(
        string? nameSearch,
        string? type,
        string? criticality,
        string? lifecycleStatus,
        int limit)
    {
        if (!TryParseOptional(type, Types, out ApplicationSystemType? parsedType)
            || !TryParseOptional(
                criticality,
                Criticalities,
                out ApplicationCriticality? parsedCriticality)
            || !TryParseOptional(
                lifecycleStatus,
                LifecycleStatuses,
                out ApplicationLifecycleStatus? parsedLifecycleStatus))
        {
            return Invalid<ListApplicationSystemsQuery>();
        }

        return ApplicationResultFactory.Success(new ListApplicationSystemsQuery(
            nameSearch,
            parsedType,
            parsedCriticality,
            parsedLifecycleStatus,
            limit));
    }

    public ApplicationResult<CreateApplicationSystemCommand> CreateCreateCommand(
        string name,
        string description,
        string type,
        string criticality,
        string initialLifecycleStatus,
        string businessOwner,
        string technicalOwner,
        string supportTeam,
        string? vendorName)
    {
        if (!TryParseName(type, Types, out ApplicationSystemType parsedType)
            || !TryParseName(
                criticality,
                Criticalities,
                out ApplicationCriticality parsedCriticality)
            || !TryParseName(
                initialLifecycleStatus,
                InitialLifecycleStatuses,
                out ApplicationLifecycleStatus parsedLifecycleStatus))
        {
            return Invalid<CreateApplicationSystemCommand>();
        }

        return ApplicationResultFactory.Success(new CreateApplicationSystemCommand(
            name,
            description,
            parsedType,
            parsedCriticality,
            parsedLifecycleStatus,
            businessOwner,
            technicalOwner,
            supportTeam,
            vendorName));
    }

    public ApplicationResult<UpdateApplicationSystemCommand> CreateUpdateCommand(
        Guid applicationSystemId,
        string name,
        string description,
        string type,
        string criticality,
        string businessOwner,
        string technicalOwner,
        string supportTeam,
        string? vendorName)
    {
        if (!TryParseName(type, Types, out ApplicationSystemType parsedType)
            || !TryParseName(
                criticality,
                Criticalities,
                out ApplicationCriticality parsedCriticality))
        {
            return Invalid<UpdateApplicationSystemCommand>();
        }

        return ApplicationResultFactory.Success(new UpdateApplicationSystemCommand(
            applicationSystemId,
            name,
            description,
            parsedType,
            parsedCriticality,
            businessOwner,
            technicalOwner,
            supportTeam,
            vendorName));
    }

    public ApplicationResult<ChangeApplicationSystemLifecycleCommand> CreateLifecycleCommand(
        Guid applicationSystemId,
        string targetLifecycleStatus,
        string? retirementReason)
    {
        if (!TryParseName(
            targetLifecycleStatus,
            LifecycleStatuses,
            out ApplicationLifecycleStatus parsedLifecycleStatus))
        {
            return Invalid<ChangeApplicationSystemLifecycleCommand>();
        }

        return ApplicationResultFactory.Success(new ChangeApplicationSystemLifecycleCommand(
            applicationSystemId,
            parsedLifecycleStatus,
            retirementReason));
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
