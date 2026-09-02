using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.WorkItems.TransitionWorkItemStatus;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.UnitTests.TestDoubles;

namespace AppSupportHub.UnitTests.Application.WorkItems;

public sealed class TransitionWorkItemStatusHandlerTests
{
    private static readonly DateTimeOffset _currentTime = new(
        2026,
        2,
        6,
        12,
        0,
        0,
        TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsyncReturnsNotFoundWithoutSavingAsync()
    {
        var repository = new InMemoryWorkItemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        TransitionWorkItemStatusHandler handler = CreateHandler(repository, unitOfWork);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new TransitionWorkItemStatusCommand(
                Guid.NewGuid(),
                WorkItemStatus.UnderAnalysis,
                "Actor",
                null,
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("work_items.not_found", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsyncTransitionsAndSavesExactlyOnceAsync()
    {
        WorkItem workItem = CreateWorkItem();
        var repository = new InMemoryWorkItemRepository();
        repository.Seed(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        TransitionWorkItemStatusHandler handler = CreateHandler(repository, unitOfWork);
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new TransitionWorkItemStatusCommand(
                workItem.Id,
                WorkItemStatus.UnderAnalysis,
                "Actor",
                "Triage started",
                null),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Changed);
        Assert.Equal(WorkItemStatus.UnderAnalysis, workItem.Status);
        Assert.Equal(_currentTime, workItem.UpdatedAtUtc);
        Assert.Equal(1, unitOfWork.SaveCallCount);
        Assert.Equal(cancellationToken, repository.GetByIdCancellationToken);
        Assert.Equal(cancellationToken, unitOfWork.SaveCancellationToken);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsInvalidTransitionWithoutSavingAsync()
    {
        WorkItem workItem = CreateWorkItem();
        var repository = new InMemoryWorkItemRepository();
        repository.Seed(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        TransitionWorkItemStatusHandler handler = CreateHandler(repository, unitOfWork);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new TransitionWorkItemStatusCommand(
                workItem.Id,
                WorkItemStatus.InProgress,
                "Actor",
                null,
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("work_items.invalid_transition", result.Error?.Code);
        Assert.Equal(ApplicationErrorType.BusinessRule, result.Error?.Type);
        Assert.Equal(0, unitOfWork.SaveCallCount);
        Assert.Equal(WorkItemStatus.New, workItem.Status);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsValidationFailureWithoutSavingAsync()
    {
        WorkItem workItem = CreateWorkItem();
        var repository = new InMemoryWorkItemRepository();
        repository.Seed(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        TransitionWorkItemStatusHandler handler = CreateHandler(repository, unitOfWork);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new TransitionWorkItemStatusCommand(
                workItem.Id,
                WorkItemStatus.Cancelled,
                "Actor",
                "   ",
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error?.Code);
        Assert.Equal(ApplicationErrorType.Validation, result.Error?.Type);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsUnchangedWithoutSavingForSameStatusAsync()
    {
        WorkItem workItem = CreateWorkItem();
        int historyCount = workItem.History.Count;
        var repository = new InMemoryWorkItemRepository();
        repository.Seed(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        TransitionWorkItemStatusHandler handler = CreateHandler(repository, unitOfWork);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new TransitionWorkItemStatusCommand(
                workItem.Id,
                WorkItemStatus.New,
                "Actor",
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Changed);
        Assert.Equal(0, unitOfWork.SaveCallCount);
        Assert.Equal(historyCount, workItem.History.Count);
    }

    private static TransitionWorkItemStatusHandler CreateHandler(
        InMemoryWorkItemRepository repository,
        RecordingUnitOfWork unitOfWork)
    {
        return new TransitionWorkItemStatusHandler(
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
