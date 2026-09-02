using AppSupportHub.Domain.Systems;

namespace AppSupportHub.UnitTests.Domain.Systems;

public sealed class ApplicationSystemTests
{
    private static readonly DateTimeOffset _createdAt = new(
        2026,
        2,
        1,
        14,
        30,
        0,
        TimeSpan.Zero);

    public static TheoryData<ApplicationLifecycleStatus, ApplicationLifecycleStatus>
        AllowedTransitions => new()
        {
            { ApplicationLifecycleStatus.Planned, ApplicationLifecycleStatus.Active },
            { ApplicationLifecycleStatus.Planned, ApplicationLifecycleStatus.Retired },
            { ApplicationLifecycleStatus.Active, ApplicationLifecycleStatus.Maintenance },
            { ApplicationLifecycleStatus.Active, ApplicationLifecycleStatus.Retired },
            { ApplicationLifecycleStatus.Maintenance, ApplicationLifecycleStatus.Active },
            { ApplicationLifecycleStatus.Maintenance, ApplicationLifecycleStatus.Retired },
        };

    public static TheoryData<ApplicationLifecycleStatus, ApplicationLifecycleStatus>
        ForbiddenTransitions => new()
        {
            { ApplicationLifecycleStatus.Planned, ApplicationLifecycleStatus.Maintenance },
            { ApplicationLifecycleStatus.Active, ApplicationLifecycleStatus.Planned },
            { ApplicationLifecycleStatus.Maintenance, ApplicationLifecycleStatus.Planned },
            { ApplicationLifecycleStatus.Retired, ApplicationLifecycleStatus.Planned },
            { ApplicationLifecycleStatus.Retired, ApplicationLifecycleStatus.Active },
            { ApplicationLifecycleStatus.Retired, ApplicationLifecycleStatus.Maintenance },
        };

    [Fact]
    public void CreateBuildsValidPlannedCustomSystem()
    {
        ApplicationSystem system = CreateSystem();

        Assert.NotEqual(Guid.Empty, system.Id);
        Assert.Equal(ApplicationLifecycleStatus.Planned, system.LifecycleStatus);
        Assert.Equal(ApplicationSystemType.Custom, system.Type);
        Assert.Null(system.VendorName);
        Assert.Null(system.RetiredAtUtc);
        Assert.Null(system.RetirementReason);
        Assert.False(system.IsRetired);
    }

    [Fact]
    public void CreateBuildsValidActiveCommercialSystem()
    {
        ApplicationSystem system = CreateSystem(
            type: ApplicationSystemType.Commercial,
            initialStatus: ApplicationLifecycleStatus.Active,
            vendorName: "Vendor");

        Assert.Equal(ApplicationLifecycleStatus.Active, system.LifecycleStatus);
        Assert.Equal("Vendor", system.VendorName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRejectsCommercialSystemWithoutVendor(string? vendorName)
    {
        Assert.Throws<ArgumentException>(() => CreateSystem(
            type: ApplicationSystemType.Commercial,
            vendorName: vendorName));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("description")]
    [InlineData("businessOwner")]
    [InlineData("technicalOwner")]
    [InlineData("supportTeam")]
    public void CreateRejectsWhitespaceRequiredText(string parameter)
    {
        Assert.Throws<ArgumentException>(() => CreateWithRequiredValue(parameter, "   "));
    }

    [Theory]
    [InlineData("name", ApplicationSystem.NameMaxLength)]
    [InlineData("description", ApplicationSystem.DescriptionMaxLength)]
    [InlineData("businessOwner", ApplicationSystem.BusinessOwnerMaxLength)]
    [InlineData("technicalOwner", ApplicationSystem.TechnicalOwnerMaxLength)]
    [InlineData("supportTeam", ApplicationSystem.SupportTeamMaxLength)]
    [InlineData("vendorName", ApplicationSystem.VendorNameMaxLength)]
    public void CreateRejectsTextBeyondEachMaximum(string parameter, int maximumLength)
    {
        string oversizedValue = new('x', maximumLength + 1);

        Assert.Throws<ArgumentException>(() => CreateWithRequiredValue(parameter, oversizedValue));
    }

    [Theory]
    [InlineData(ApplicationLifecycleStatus.Maintenance)]
    [InlineData(ApplicationLifecycleStatus.Retired)]
    public void CreateRejectsInvalidInitialLifecycleStatus(ApplicationLifecycleStatus status)
    {
        Assert.Throws<ArgumentException>(() => CreateSystem(initialStatus: status));
    }

    [Fact]
    public void CreateTrimsTextAndNormalizesTimestampsToUtc()
    {
        DateTimeOffset localTimestamp = new(2026, 2, 1, 9, 30, 0, TimeSpan.FromHours(-5));

        var system = ApplicationSystem.Create(
            " System ",
            " Description ",
            ApplicationSystemType.Commercial,
            ApplicationCriticality.High,
            ApplicationLifecycleStatus.Active,
            " Business ",
            " Technical ",
            " Support ",
            " Vendor ",
            localTimestamp);

        Assert.Equal("System", system.Name);
        Assert.Equal("Description", system.Description);
        Assert.Equal("Business", system.BusinessOwner);
        Assert.Equal("Technical", system.TechnicalOwner);
        Assert.Equal("Support", system.SupportTeam);
        Assert.Equal("Vendor", system.VendorName);
        Assert.Equal(localTimestamp.ToUniversalTime(), system.CreatedAtUtc);
        Assert.Equal(system.CreatedAtUtc, system.UpdatedAtUtc);
        Assert.Equal(TimeSpan.Zero, system.CreatedAtUtc.Offset);
    }

    [Theory]
    [MemberData(nameof(AllowedTransitions))]
    public void TransitionLifecycleAllowsExactMatrix(
        ApplicationLifecycleStatus currentStatus,
        ApplicationLifecycleStatus targetStatus)
    {
        ApplicationSystem system = CreateAtStatus(currentStatus);
        DateTimeOffset transitionedAt = _createdAt.AddHours(1);

        bool changed = system.TransitionLifecycle(
            targetStatus,
            transitionedAt,
            targetStatus == ApplicationLifecycleStatus.Retired ? " No longer needed " : null);

        Assert.True(changed);
        Assert.Equal(targetStatus, system.LifecycleStatus);
        Assert.Equal(transitionedAt, system.UpdatedAtUtc);

        if (targetStatus == ApplicationLifecycleStatus.Retired)
        {
            Assert.True(system.IsRetired);
            Assert.Equal(transitionedAt, system.RetiredAtUtc);
            Assert.Equal("No longer needed", system.RetirementReason);
        }
    }

    [Theory]
    [MemberData(nameof(ForbiddenTransitions))]
    public void TransitionLifecycleRejectsEveryForbiddenTransition(
        ApplicationLifecycleStatus currentStatus,
        ApplicationLifecycleStatus targetStatus)
    {
        ApplicationSystem system = CreateAtStatus(currentStatus);
        DateTimeOffset originalUpdatedAt = system.UpdatedAtUtc;

        Assert.Throws<InvalidOperationException>(() => system.TransitionLifecycle(
            targetStatus,
            _createdAt.AddHours(2)));
        Assert.Equal(currentStatus, system.LifecycleStatus);
        Assert.Equal(originalUpdatedAt, system.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RetirementRequiresReason(string? retirementReason)
    {
        ApplicationSystem system = CreateSystem();

        Assert.Throws<ArgumentException>(() => system.TransitionLifecycle(
            ApplicationLifecycleStatus.Retired,
            _createdAt.AddHours(1),
            retirementReason));
    }

    [Fact]
    public void RetirementRejectsReasonBeyondMaximum()
    {
        ApplicationSystem system = CreateSystem();
        string oversizedReason = new('x', ApplicationSystem.RetirementReasonMaxLength + 1);

        Assert.Throws<ArgumentException>(() => system.TransitionLifecycle(
            ApplicationLifecycleStatus.Retired,
            _createdAt.AddHours(1),
            oversizedReason));
    }

    [Fact]
    public void RetiredSystemCannotReactivate()
    {
        ApplicationSystem system = CreateAtStatus(ApplicationLifecycleStatus.Retired);

        Assert.Throws<InvalidOperationException>(() => system.TransitionLifecycle(
            ApplicationLifecycleStatus.Active,
            _createdAt.AddHours(2)));
    }

    [Fact]
    public void SameLifecycleStatusIsTrueNoOp()
    {
        ApplicationSystem system = CreateAtStatus(ApplicationLifecycleStatus.Active);
        DateTimeOffset originalUpdatedAt = system.UpdatedAtUtc;

        bool changed = system.TransitionLifecycle(
            ApplicationLifecycleStatus.Active,
            _createdAt.AddHours(2));

        Assert.False(changed);
        Assert.Equal(originalUpdatedAt, system.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateMetadataChangesAllSupportedValues()
    {
        ApplicationSystem system = CreateSystem();
        DateTimeOffset updatedAt = _createdAt.AddDays(1);

        bool changed = system.UpdateMetadata(
            " Updated ",
            " Updated description ",
            ApplicationSystemType.Commercial,
            ApplicationCriticality.Critical,
            " New business owner ",
            " New technical owner ",
            " New support team ",
            " New vendor ",
            updatedAt);

        Assert.True(changed);
        Assert.Equal("Updated", system.Name);
        Assert.Equal("Updated description", system.Description);
        Assert.Equal(ApplicationSystemType.Commercial, system.Type);
        Assert.Equal(ApplicationCriticality.Critical, system.Criticality);
        Assert.Equal("New business owner", system.BusinessOwner);
        Assert.Equal("New technical owner", system.TechnicalOwner);
        Assert.Equal("New support team", system.SupportTeam);
        Assert.Equal("New vendor", system.VendorName);
        Assert.Equal(updatedAt, system.UpdatedAtUtc);
    }

    [Fact]
    public void UnchangedMetadataIsTrueNoOp()
    {
        ApplicationSystem system = CreateSystem();
        DateTimeOffset originalUpdatedAt = system.UpdatedAtUtc;

        bool changed = system.UpdateMetadata(
            $" {system.Name} ",
            $" {system.Description} ",
            system.Type,
            system.Criticality,
            $" {system.BusinessOwner} ",
            $" {system.TechnicalOwner} ",
            $" {system.SupportTeam} ",
            "   ",
            _createdAt.AddDays(1));

        Assert.False(changed);
        Assert.Equal(originalUpdatedAt, system.UpdatedAtUtc);
    }

    [Fact]
    public void MetadataUpdateEnforcesCommercialVendorRule()
    {
        ApplicationSystem system = CreateSystem();

        Assert.Throws<ArgumentException>(() => system.UpdateMetadata(
            system.Name,
            system.Description,
            ApplicationSystemType.Commercial,
            system.Criticality,
            system.BusinessOwner,
            system.TechnicalOwner,
            system.SupportTeam,
            null,
            _createdAt.AddDays(1)));
        Assert.Equal(ApplicationSystemType.Custom, system.Type);
    }

    [Fact]
    public void NonRetirementTransitionRejectsRetirementReason()
    {
        ApplicationSystem system = CreateSystem();

        Assert.Throws<ArgumentException>(() => system.TransitionLifecycle(
            ApplicationLifecycleStatus.Active,
            _createdAt.AddHours(1),
            "Not applicable"));
        Assert.Equal(ApplicationLifecycleStatus.Planned, system.LifecycleStatus);
    }

    private static ApplicationSystem CreateSystem(
        ApplicationSystemType type = ApplicationSystemType.Custom,
        ApplicationLifecycleStatus initialStatus = ApplicationLifecycleStatus.Planned,
        string? vendorName = null)
    {
        return ApplicationSystem.Create(
            "Payroll",
            "Payroll application",
            type,
            ApplicationCriticality.High,
            initialStatus,
            "Finance",
            "Technology",
            "Business Applications",
            vendorName,
            _createdAt);
    }

    private static ApplicationSystem CreateAtStatus(ApplicationLifecycleStatus status)
    {
        ApplicationSystem system = CreateSystem(
            initialStatus: status == ApplicationLifecycleStatus.Planned
                ? ApplicationLifecycleStatus.Planned
                : ApplicationLifecycleStatus.Active);

        if (status == ApplicationLifecycleStatus.Maintenance)
        {
            system.TransitionLifecycle(ApplicationLifecycleStatus.Maintenance, _createdAt.AddMinutes(10));
        }
        else if (status == ApplicationLifecycleStatus.Retired)
        {
            system.TransitionLifecycle(
                ApplicationLifecycleStatus.Retired,
                _createdAt.AddMinutes(10),
                "Retired for test");
        }

        return system;
    }

    private static ApplicationSystem CreateWithRequiredValue(string parameter, string value)
    {
        return ApplicationSystem.Create(
            parameter == "name" ? value : "Payroll",
            parameter == "description" ? value : "Payroll application",
            parameter == "vendorName" ? ApplicationSystemType.Commercial : ApplicationSystemType.Custom,
            ApplicationCriticality.High,
            ApplicationLifecycleStatus.Planned,
            parameter == "businessOwner" ? value : "Finance",
            parameter == "technicalOwner" ? value : "Technology",
            parameter == "supportTeam" ? value : "Business Applications",
            parameter == "vendorName" ? value : null,
            _createdAt);
    }
}
