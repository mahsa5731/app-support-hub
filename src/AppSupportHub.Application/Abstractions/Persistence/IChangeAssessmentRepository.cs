using AppSupportHub.Domain.ChangeAssessments;

namespace AppSupportHub.Application.Abstractions.Persistence;

public interface IChangeAssessmentRepository
{
    Task<ChangeAssessment?> GetByWorkItemIdAsync(
        Guid workItemId,
        CancellationToken cancellationToken);

    Task AddAsync(ChangeAssessment assessment, CancellationToken cancellationToken);
}
