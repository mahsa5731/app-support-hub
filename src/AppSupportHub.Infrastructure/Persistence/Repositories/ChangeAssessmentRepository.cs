using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Domain.ChangeAssessments;
using Microsoft.EntityFrameworkCore;

namespace AppSupportHub.Infrastructure.Persistence.Repositories;

public sealed class ChangeAssessmentRepository(AppSupportHubDbContext dbContext)
    : IChangeAssessmentRepository
{
    public Task<ChangeAssessment?> GetByWorkItemIdAsync(
        Guid workItemId,
        CancellationToken cancellationToken)
    {
        return dbContext.ChangeAssessments.SingleOrDefaultAsync(
            assessment => assessment.WorkItemId == workItemId,
            cancellationToken);
    }

    public async Task AddAsync(
        ChangeAssessment assessment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        await dbContext.ChangeAssessments.AddAsync(assessment, cancellationToken);
    }
}
