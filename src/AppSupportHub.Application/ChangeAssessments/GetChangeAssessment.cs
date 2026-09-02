using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Domain.ChangeAssessments;
using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.Application.ChangeAssessments;

public sealed record GetChangeAssessmentQuery(Guid WorkItemId);

public sealed record ChangeAssessmentReadModel(
    Guid Id,
    string BusinessNeed,
    string TechnicalImpact,
    string SecurityImpact,
    string Risk,
    string AcceptanceCriteria,
    string TestPlan,
    string RollbackPlan,
    string AssessedByIdentifier,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record GetChangeAssessmentResult(
    Guid WorkItemId,
    string WorkItemTitle,
    ChangeAssessmentReadModel? Assessment);

public sealed class GetChangeAssessmentHandler(
    IWorkItemRepository workItemRepository,
    IChangeAssessmentRepository assessmentRepository)
{
    public async Task<ApplicationResult<GetChangeAssessmentResult>> ExecuteAsync(
        GetChangeAssessmentQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        WorkItem? workItem = await workItemRepository.GetByIdAsync(
            query.WorkItemId,
            cancellationToken);
        if (workItem is null)
        {
            return ApplicationResultFactory.Failure<GetChangeAssessmentResult>(NotFound());
        }

        if (workItem.Type != WorkItemType.ChangeRequest)
        {
            return ApplicationResultFactory.Failure<GetChangeAssessmentResult>(WrongType());
        }

        ChangeAssessment? assessment = await assessmentRepository.GetByWorkItemIdAsync(
            query.WorkItemId,
            cancellationToken);
        ChangeAssessmentReadModel? readModel = assessment is null
            ? null
            : new ChangeAssessmentReadModel(
                assessment.Id,
                assessment.BusinessNeed,
                assessment.TechnicalImpact,
                assessment.SecurityImpact,
                assessment.Risk.ToString(),
                assessment.AcceptanceCriteria,
                assessment.TestPlan,
                assessment.RollbackPlan,
                assessment.AssessedByIdentifier,
                assessment.CreatedAtUtc,
                assessment.UpdatedAtUtc);
        return ApplicationResultFactory.Success(new GetChangeAssessmentResult(
            workItem.Id,
            workItem.Title,
            readModel));
    }

    internal static ApplicationError NotFound() => new(
        "work_items.not_found",
        "The work item was not found.",
        ApplicationErrorType.NotFound);

    internal static ApplicationError WrongType() => new(
        "change_assessments.wrong_work_item_type",
        "Only change-request work items can have an assessment.",
        ApplicationErrorType.BusinessRule);
}
