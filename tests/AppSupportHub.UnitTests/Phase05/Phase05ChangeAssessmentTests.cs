using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.ChangeAssessments;
using AppSupportHub.Domain.ChangeAssessments;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.UnitTests.TestDoubles;

namespace AppSupportHub.UnitTests.Phase05;

public sealed class Phase05ChangeAssessmentTests
{
    private static readonly DateTimeOffset _createdAt =
        new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AssessmentCreateUpdateIdempotencyAndValidationStayInDomain()
    {
        var assessment = ChangeAssessment.Create(
            Guid.NewGuid(),
            "  Business need  ",
            "Technical impact",
            "Security impact",
            ChangeRisk.Medium,
            "Acceptance criteria",
            "Test plan",
            "Rollback plan",
            "  demo.actor  ",
            _createdAt);

        Assert.Equal("Business need", assessment.BusinessNeed);
        Assert.Equal("demo.actor", assessment.AssessedByIdentifier);
        Assert.Equal(_createdAt, assessment.CreatedAtUtc);
        Assert.Equal(_createdAt, assessment.UpdatedAtUtc);
        Assert.False(assessment.Update(
            "Business need", "Technical impact", "Security impact", ChangeRisk.Medium,
            "Acceptance criteria", "Test plan", "Rollback plan", "demo.actor",
            _createdAt.AddHours(1)));
        Assert.Equal(_createdAt, assessment.UpdatedAtUtc);

        Assert.True(assessment.Update(
            "Revised need", "Technical impact", "Security impact", ChangeRisk.High,
            "Acceptance criteria", "Test plan", "Rollback plan", "demo.actor",
            _createdAt.AddHours(2)));
        Assert.Equal(_createdAt, assessment.CreatedAtUtc);
        Assert.Equal(_createdAt.AddHours(2), assessment.UpdatedAtUtc);
        Assert.Throws<ArgumentException>(() => ChangeAssessment.Create(
            Guid.NewGuid(), string.Empty, "Technical", "Security", ChangeRisk.Low,
            "Acceptance", "Test", "Rollback", "actor", _createdAt));
        Assert.Throws<ArgumentException>(() => assessment.Update(
            new string('x', 2001), "Technical", "Security", ChangeRisk.Low,
            "Acceptance", "Test", "Rollback", "actor", _createdAt.AddHours(3)));
    }

    [Fact]
    public async Task SaveHandlerRejectsNonChangeRequestWithoutSavingAsync()
    {
        var workItems = new InMemoryWorkItemRepository();
        var incident = WorkItem.Create(
            Guid.NewGuid(), WorkItemType.Incident, "Incident", "Synthetic incident.",
            WorkItemPriority.Medium, null, "creator", _createdAt);
        workItems.Seed(incident);
        var assessments = new InMemoryAssessmentRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new SaveChangeAssessmentHandler(
            workItems, assessments, unitOfWork, new FixedTimeProvider(_createdAt));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new SaveChangeAssessmentCommand(
                incident.Id, "Need", "Technical", "Security", ChangeRisk.Medium,
                "Acceptance", "Test", "Rollback", "demo.actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("change_assessments.wrong_work_item_type", result.Error!.Code);
        Assert.Equal(0, assessments.AddCallCount);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    private sealed class InMemoryAssessmentRepository : IChangeAssessmentRepository
    {
        public int AddCallCount { get; private set; }

        public Task<ChangeAssessment?> GetByWorkItemIdAsync(
            Guid workItemId,
            CancellationToken cancellationToken) => Task.FromResult<ChangeAssessment?>(null);

        public Task AddAsync(
            ChangeAssessment assessment,
            CancellationToken cancellationToken)
        {
            AddCallCount++;
            return Task.CompletedTask;
        }
    }
}
