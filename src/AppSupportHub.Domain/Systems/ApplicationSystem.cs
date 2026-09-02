namespace AppSupportHub.Domain.Systems;

public sealed class ApplicationSystem
{
    public const int NameMaxLength = 150;
    public const int DescriptionMaxLength = 1000;
    public const int BusinessOwnerMaxLength = 150;
    public const int TechnicalOwnerMaxLength = 150;
    public const int SupportTeamMaxLength = 150;
    public const int VendorNameMaxLength = 150;
    public const int RetirementReasonMaxLength = 500;

    private ApplicationSystem(
        Guid id,
        string name,
        string description,
        ApplicationSystemType type,
        ApplicationCriticality criticality,
        ApplicationLifecycleStatus lifecycleStatus,
        string businessOwner,
        string technicalOwner,
        string supportTeam,
        string? vendorName,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        Description = description;
        Type = type;
        Criticality = criticality;
        LifecycleStatus = lifecycleStatus;
        BusinessOwner = businessOwner;
        TechnicalOwner = technicalOwner;
        SupportTeam = supportTeam;
        VendorName = vendorName;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public ApplicationSystemType Type { get; private set; }

    public ApplicationCriticality Criticality { get; private set; }

    public ApplicationLifecycleStatus LifecycleStatus { get; private set; }

    public string BusinessOwner { get; private set; }

    public string TechnicalOwner { get; private set; }

    public string SupportTeam { get; private set; }

    public string? VendorName { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? RetiredAtUtc { get; private set; }

    public string? RetirementReason { get; private set; }

    public bool IsRetired => LifecycleStatus == ApplicationLifecycleStatus.Retired;

    public static ApplicationSystem Create(
        string name,
        string description,
        ApplicationSystemType type,
        ApplicationCriticality criticality,
        ApplicationLifecycleStatus initialLifecycleStatus,
        string businessOwner,
        string technicalOwner,
        string supportTeam,
        string? vendorName,
        DateTimeOffset createdAt)
    {
        ValidateEnum(type, nameof(type));
        ValidateEnum(criticality, nameof(criticality));
        ValidateInitialLifecycleStatus(initialLifecycleStatus);

        string normalizedName = NormalizeRequired(name, NameMaxLength, nameof(name));
        string normalizedDescription = NormalizeRequired(
            description,
            DescriptionMaxLength,
            nameof(description));
        string normalizedBusinessOwner = NormalizeRequired(
            businessOwner,
            BusinessOwnerMaxLength,
            nameof(businessOwner));
        string normalizedTechnicalOwner = NormalizeRequired(
            technicalOwner,
            TechnicalOwnerMaxLength,
            nameof(technicalOwner));
        string normalizedSupportTeam = NormalizeRequired(
            supportTeam,
            SupportTeamMaxLength,
            nameof(supportTeam));
        string? normalizedVendorName = NormalizeOptional(
            vendorName,
            VendorNameMaxLength,
            nameof(vendorName));

        ValidateVendor(type, normalizedVendorName);

        return new ApplicationSystem(
            Guid.NewGuid(),
            normalizedName,
            normalizedDescription,
            type,
            criticality,
            initialLifecycleStatus,
            normalizedBusinessOwner,
            normalizedTechnicalOwner,
            normalizedSupportTeam,
            normalizedVendorName,
            createdAt.ToUniversalTime());
    }

    public bool TransitionLifecycle(
        ApplicationLifecycleStatus targetStatus,
        DateTimeOffset transitionedAt,
        string? retirementReason = null)
    {
        ValidateEnum(targetStatus, nameof(targetStatus));

        if (targetStatus == LifecycleStatus)
        {
            return false;
        }

        string? normalizedRetirementReason = null;

        if (targetStatus == ApplicationLifecycleStatus.Retired)
        {
            normalizedRetirementReason = NormalizeRequired(
                retirementReason,
                RetirementReasonMaxLength,
                nameof(retirementReason));
        }
        else if (retirementReason is not null)
        {
            throw new ArgumentException(
                "A retirement reason is accepted only when retiring an application system.",
                nameof(retirementReason));
        }

        if (!CanTransitionTo(targetStatus))
        {
            throw new InvalidOperationException(
                $"Application system cannot transition from {LifecycleStatus} to {targetStatus}.");
        }

        DateTimeOffset normalizedTimestamp = transitionedAt.ToUniversalTime();
        LifecycleStatus = targetStatus;
        UpdatedAtUtc = normalizedTimestamp;

        if (targetStatus == ApplicationLifecycleStatus.Retired)
        {
            RetiredAtUtc = normalizedTimestamp;
            RetirementReason = normalizedRetirementReason;
        }

        return true;
    }

    public bool UpdateMetadata(
        string name,
        string description,
        ApplicationSystemType type,
        ApplicationCriticality criticality,
        string businessOwner,
        string technicalOwner,
        string supportTeam,
        string? vendorName,
        DateTimeOffset updatedAt)
    {
        ValidateEnum(type, nameof(type));
        ValidateEnum(criticality, nameof(criticality));

        string normalizedName = NormalizeRequired(name, NameMaxLength, nameof(name));
        string normalizedDescription = NormalizeRequired(
            description,
            DescriptionMaxLength,
            nameof(description));
        string normalizedBusinessOwner = NormalizeRequired(
            businessOwner,
            BusinessOwnerMaxLength,
            nameof(businessOwner));
        string normalizedTechnicalOwner = NormalizeRequired(
            technicalOwner,
            TechnicalOwnerMaxLength,
            nameof(technicalOwner));
        string normalizedSupportTeam = NormalizeRequired(
            supportTeam,
            SupportTeamMaxLength,
            nameof(supportTeam));
        string? normalizedVendorName = NormalizeOptional(
            vendorName,
            VendorNameMaxLength,
            nameof(vendorName));

        ValidateVendor(type, normalizedVendorName);

        if (normalizedName == Name
            && normalizedDescription == Description
            && type == Type
            && criticality == Criticality
            && normalizedBusinessOwner == BusinessOwner
            && normalizedTechnicalOwner == TechnicalOwner
            && normalizedSupportTeam == SupportTeam
            && normalizedVendorName == VendorName)
        {
            return false;
        }

        Name = normalizedName;
        Description = normalizedDescription;
        Type = type;
        Criticality = criticality;
        BusinessOwner = normalizedBusinessOwner;
        TechnicalOwner = normalizedTechnicalOwner;
        SupportTeam = normalizedSupportTeam;
        VendorName = normalizedVendorName;
        UpdatedAtUtc = updatedAt.ToUniversalTime();

        return true;
    }

    public bool CanTransitionTo(ApplicationLifecycleStatus targetStatus)
    {
        if (!Enum.IsDefined(targetStatus))
        {
            return false;
        }

        return LifecycleStatus switch
        {
            ApplicationLifecycleStatus.Planned => targetStatus is
                ApplicationLifecycleStatus.Active or ApplicationLifecycleStatus.Retired,
            ApplicationLifecycleStatus.Active => targetStatus is
                ApplicationLifecycleStatus.Maintenance or ApplicationLifecycleStatus.Retired,
            ApplicationLifecycleStatus.Maintenance => targetStatus is
                ApplicationLifecycleStatus.Active or ApplicationLifecycleStatus.Retired,
            ApplicationLifecycleStatus.Retired => false,
            _ => false,
        };
    }

    private static void ValidateInitialLifecycleStatus(ApplicationLifecycleStatus lifecycleStatus)
    {
        ValidateEnum(lifecycleStatus, nameof(lifecycleStatus));

        if (lifecycleStatus is not ApplicationLifecycleStatus.Planned
            and not ApplicationLifecycleStatus.Active)
        {
            throw new ArgumentException(
                "Initial lifecycle status must be Planned or Active.",
                nameof(lifecycleStatus));
        }
    }

    private static void ValidateVendor(ApplicationSystemType type, string? vendorName)
    {
        if (type == ApplicationSystemType.Commercial && vendorName is null)
        {
            throw new ArgumentException(
                "Commercial application systems require a vendor name.",
                nameof(vendorName));
        }
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

    private static void ValidateEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value is not defined.");
        }
    }
}
