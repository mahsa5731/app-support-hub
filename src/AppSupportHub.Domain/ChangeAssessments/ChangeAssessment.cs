namespace AppSupportHub.Domain.ChangeAssessments;

public enum ChangeRisk
{
    Low,
    Medium,
    High,
    Critical,
}

public sealed class ChangeAssessment
{
    public const int NarrativeMaxLength = 2000;
    public const int AssessedByMaxLength = 200;

    private ChangeAssessment(
        Guid id,
        Guid workItemId,
        string businessNeed,
        string technicalImpact,
        string securityImpact,
        ChangeRisk risk,
        string acceptanceCriteria,
        string testPlan,
        string rollbackPlan,
        string assessedByIdentifier,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        WorkItemId = workItemId;
        BusinessNeed = businessNeed;
        TechnicalImpact = technicalImpact;
        SecurityImpact = securityImpact;
        Risk = risk;
        AcceptanceCriteria = acceptanceCriteria;
        TestPlan = testPlan;
        RollbackPlan = rollbackPlan;
        AssessedByIdentifier = assessedByIdentifier;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid WorkItemId { get; }

    public string BusinessNeed { get; private set; }

    public string TechnicalImpact { get; private set; }

    public string SecurityImpact { get; private set; }

    public ChangeRisk Risk { get; private set; }

    public string AcceptanceCriteria { get; private set; }

    public string TestPlan { get; private set; }

    public string RollbackPlan { get; private set; }

    public string AssessedByIdentifier { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static ChangeAssessment Create(
        Guid workItemId,
        string businessNeed,
        string technicalImpact,
        string securityImpact,
        ChangeRisk risk,
        string acceptanceCriteria,
        string testPlan,
        string rollbackPlan,
        string assessedByIdentifier,
        DateTimeOffset createdAt)
    {
        if (workItemId == Guid.Empty)
        {
            throw new ArgumentException("A work-item identifier is required.", nameof(workItemId));
        }

        ValidatedValues values = Validate(
            businessNeed,
            technicalImpact,
            securityImpact,
            risk,
            acceptanceCriteria,
            testPlan,
            rollbackPlan,
            assessedByIdentifier);
        return new ChangeAssessment(
            Guid.NewGuid(),
            workItemId,
            values.BusinessNeed,
            values.TechnicalImpact,
            values.SecurityImpact,
            risk,
            values.AcceptanceCriteria,
            values.TestPlan,
            values.RollbackPlan,
            values.AssessedByIdentifier,
            createdAt.ToUniversalTime());
    }

    public bool Update(
        string businessNeed,
        string technicalImpact,
        string securityImpact,
        ChangeRisk risk,
        string acceptanceCriteria,
        string testPlan,
        string rollbackPlan,
        string assessedByIdentifier,
        DateTimeOffset updatedAt)
    {
        ValidatedValues values = Validate(
            businessNeed,
            technicalImpact,
            securityImpact,
            risk,
            acceptanceCriteria,
            testPlan,
            rollbackPlan,
            assessedByIdentifier);
        if (values.BusinessNeed == BusinessNeed
            && values.TechnicalImpact == TechnicalImpact
            && values.SecurityImpact == SecurityImpact
            && risk == Risk
            && values.AcceptanceCriteria == AcceptanceCriteria
            && values.TestPlan == TestPlan
            && values.RollbackPlan == RollbackPlan
            && values.AssessedByIdentifier == AssessedByIdentifier)
        {
            return false;
        }

        BusinessNeed = values.BusinessNeed;
        TechnicalImpact = values.TechnicalImpact;
        SecurityImpact = values.SecurityImpact;
        Risk = risk;
        AcceptanceCriteria = values.AcceptanceCriteria;
        TestPlan = values.TestPlan;
        RollbackPlan = values.RollbackPlan;
        AssessedByIdentifier = values.AssessedByIdentifier;
        UpdatedAtUtc = updatedAt.ToUniversalTime();
        return true;
    }

    private static ValidatedValues Validate(
        string businessNeed,
        string technicalImpact,
        string securityImpact,
        ChangeRisk risk,
        string acceptanceCriteria,
        string testPlan,
        string rollbackPlan,
        string assessedByIdentifier)
    {
        if (!Enum.IsDefined(risk))
        {
            throw new ArgumentOutOfRangeException(nameof(risk), risk, "Risk is not defined.");
        }

        return new ValidatedValues(
            NormalizeRequired(businessNeed, NarrativeMaxLength, nameof(businessNeed)),
            NormalizeRequired(technicalImpact, NarrativeMaxLength, nameof(technicalImpact)),
            NormalizeRequired(securityImpact, NarrativeMaxLength, nameof(securityImpact)),
            NormalizeRequired(acceptanceCriteria, NarrativeMaxLength, nameof(acceptanceCriteria)),
            NormalizeRequired(testPlan, NarrativeMaxLength, nameof(testPlan)),
            NormalizeRequired(rollbackPlan, NarrativeMaxLength, nameof(rollbackPlan)),
            NormalizeRequired(
                assessedByIdentifier,
                AssessedByMaxLength,
                nameof(assessedByIdentifier)));
    }

    private static string NormalizeRequired(string? value, int maximumLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", name);
        }

        string normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", name);
        }

        return normalized;
    }

    private sealed record ValidatedValues(
        string BusinessNeed,
        string TechnicalImpact,
        string SecurityImpact,
        string AcceptanceCriteria,
        string TestPlan,
        string RollbackPlan,
        string AssessedByIdentifier);
}
