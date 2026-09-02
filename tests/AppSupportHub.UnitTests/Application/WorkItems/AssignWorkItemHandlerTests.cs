using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.WorkItems.AssignWorkItem;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.UnitTests.TestDoubles;

namespace AppSupportHub.UnitTests.Application.WorkItems;

public sealed class AssignWorkItemHandlerTests
{
    private static readonly DateTimeOffset _currentTime = new(
        2026,
        2,
        6,
        11,
        0,
        0,
        TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsyncReturnsNotFoundWithoutSavingAsync()
    {
        var repository = new InMemoryWorkItemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        AssignWorkItemHandler handler = CreateHandler(repository, unitOfWork);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new AssignWorkItemCommand(Guid.NewGuid(), "Analyst", "Actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("work_items.not_found", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsyncAssignsAndSavesExactlyOnceAsync()
    {
        WorkItem workItem = CreateWorkItem();
        var repository = new InMemoryWorkItemRepository();
        repository.Seed(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        AssignWorkItemHandler handler = CreateHandler(repository, unitOfWork);
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new AssignWorkItemCommand(workItem.Id, " Analyst ", " Actor "),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Changed);
        Assert.Equal("Analyst", workItem.AssigneeIdentifier);
        Assert.Equal(_currentTime, workItem.UpdatedAtUtc);
        Assert.Equal(1, unitOfWork.SaveCallCount);
        Assert.Equal(cancellationToken, repository.GetByIdCancellationToken);
        Assert.Equal(cancellationToken, unitOfWork.SaveCancellationToken);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsValidationFailureWithoutSavingAsync()
    {
        WorkItem workItem = CreateWorkItem();
        var repository = new InMemoryWorkItemRepository();
        repository.Seed(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        AssignWorkItemHandler handler = CreateHandler(repository, unitOfWork);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new AssignWorkItemCommand(workItem.Id, "   ", "Actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsBusinessRuleForTerminalStateWithoutSavingAsync()
    {
        WorkItem workItem = CreateWorkItem();
        workItem.TransitionTo(
            WorkItemStatus.Cancelled,
            "Actor",
            _currentTime.AddMinutes(-1),
            "Cancelled");
        var repository = new InMemoryWorkItemRepository();
        repository.Seed(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        AssignWorkItemHandler handler = CreateHandler(repository, unitOfWork);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new AssignWorkItemCommand(workItem.Id, "Analyst", "Actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("work_items.assignment_forbidden", result.Error?.Code);
        Assert.Equal(ApplicationErrorType.BusinessRule, result.Error?.Type);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsUnchangedWithoutSavingForSameAssigneeAsync()
    {
        WorkItem workItem = CreateWorkItem();
        workItem.Assign("Analyst", "Actor", _currentTime.AddMinutes(-1));
        int historyCount = workItem.History.Count;
        var repository = new InMemoryWorkItemRepository();
        repository.Seed(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        AssignWorkItemHandler handler = CreateHandler(repository, unitOfWork);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new AssignWorkItemCommand(workItem.Id, " Analyst ", "Other actor"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Changed);
        Assert.Equal(0, unitOfWork.SaveCallCount);
        Assert.Equal(historyCount, workItem.History.Count);
    }

    private static AssignWorkItemHandler CreateHandler(
        InMemoryWorkItemRepository repository,
        RecordingUnitOfWork unitOfWork)
    {
        return new AssignWorkItemHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));
    }

    private static WorkItem CreateWorkItem()
    {
        return WorkItem.Create(
            Guid.NewGuid(),
            WorkItemType.Incident,
            "Incident",
            "Description",
            WorkItemPriority.High,
            null,
            "Creator",
            _currentTime.AddDays(-1));
    }
}
