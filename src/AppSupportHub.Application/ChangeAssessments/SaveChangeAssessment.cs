using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Domain.ChangeAssessments;
using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.Application.ChangeAssessments;

public sealed record SaveChangeAssessmentCommand(
    Guid WorkItemId,
    string BusinessNeed,
    string TechnicalImpact,
    string SecurityImpact,
    ChangeRisk Risk,
    string AcceptanceCriteria,
    string TestPlan,
    string RollbackPlan,
    string AssessedByIdentifier);

public sealed class ChangeAssessmentInputFactory
{
    public IReadOnlyList<string> Risks { get; } =
        Array.AsReadOnly(["Low", "Medium", "High", "Critical"]);

    public ApplicationResult<SaveChangeAssessmentCommand> CreateSaveCommand(
        Guid workItemId,
        string businessNeed,
        string technicalImpact,
        string securityImpact,
        string risk,
        string acceptanceCriteria,
        string testPlan,
        string rollbackPlan,
        string assessedByIdentifier)
    {
        string normalizedRisk = risk?.Trim() ?? string.Empty;
        if (!Risks.Any(value => string.Equals(
                value,
                normalizedRisk,
                StringComparison.OrdinalIgnoreCase))
            || !Enum.TryParse(normalizedRisk, true, out ChangeRisk parsedRisk))
        {
            return ApplicationResultFactory.Failure<SaveChangeAssessmentCommand>(new ApplicationError(
                "validation.invalid_input",
                "The selected change risk is invalid.",
                ApplicationErrorType.Validation));
        }

        return ApplicationResultFactory.Success(new SaveChangeAssessmentCommand(
            workItemId,
            businessNeed,
            technicalImpact,
            securityImpact,
            parsedRisk,
            acceptanceCriteria,
            testPlan,
            rollbackPlan,
            assessedByIdentifier));
    }
}

public sealed class SaveChangeAssessmentHandler(
    IWorkItemRepository workItemRepository,
    IChangeAssessmentRepository assessmentRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<MutationOutcome>> ExecuteAsync(
        SaveChangeAssessmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WorkItem? workItem = await workItemRepository.GetByIdAsync(
            command.WorkItemId,
            cancellationToken);
        if (workItem is null)
        {
            return ApplicationResultFactory.Failure<MutationOutcome>(
                GetChangeAssessmentHandler.NotFound());
        }

        if (workItem.Type != WorkItemType.ChangeRequest)
        {
            return ApplicationResultFactory.Failure<MutationOutcome>(
                GetChangeAssessmentHandler.WrongType());
        }

        ChangeAssessment? assessment = await assessmentRepository.GetByWorkItemIdAsync(
            command.WorkItemId,
            cancellationToken);
        bool changed;
        try
        {
            if (assessment is null)
            {
                assessment = ChangeAssessment.Create(
                    command.WorkItemId,
                    command.BusinessNeed,
                    command.TechnicalImpact,
                    command.SecurityImpact,
                    command.Risk,
                    command.AcceptanceCriteria,
                    command.TestPlan,
                    command.RollbackPlan,
                    command.AssessedByIdentifier,
                    timeProvider.GetUtcNow());
                await assessmentRepository.AddAsync(assessment, cancellationToken);
                changed = true;
            }
            else
            {
                changed = assessment.Update(
                    command.BusinessNeed,
                    command.TechnicalImpact,
                    command.SecurityImpact,
                    command.Risk,
                    command.AcceptanceCriteria,
                    command.TestPlan,
                    command.RollbackPlan,
                    command.AssessedByIdentifier,
                    timeProvider.GetUtcNow());
            }
        }
        catch (ArgumentException exception)
        {
            return ApplicationResultFactory.Failure<MutationOutcome>(new ApplicationError(
                "validation.invalid_input",
                exception.Message,
                ApplicationErrorType.Validation));
        }

        if (changed)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApplicationResultFactory.Success(new MutationOutcome(changed));
    }
}
