using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.WorkItems.ChangeWorkItemDueDate;
using AppSupportHub.Application.WorkItems.ChangeWorkItemPriority;
using AppSupportHub.Application.WorkItems.UnassignWorkItem;
using AppSupportHub.Application.WorkItems.UpdateWorkItemDetails;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.UnitTests.TestDoubles;

namespace AppSupportHub.UnitTests.Application.WorkItems;

public sealed class MissingWorkItemMutationHandlerTests
{
    private static readonly DateTimeOffset _currentTime =
        new(2026, 4, 11, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UpdateDetailsUpdatesAndSavesExactlyOnceAsync()
    {
        WorkItem workItem = CreateWorkItem();
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UpdateWorkItemDetailsHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new UpdateWorkItemDetailsCommand(
                workItem.Id,
                " Updated title ",
                " Updated description ",
                " analyst "),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Changed);
        Assert.Equal("Updated title", workItem.Title);
        Assert.Equal(_currentTime, workItem.UpdatedAtUtc);
        Assert.Equal(1, unitOfWork.SaveCallCount);
        Assert.Equal(cancellationToken, repository.GetByIdCancellationToken);
        Assert.Equal(cancellationToken, unitOfWork.SaveCancellationToken);
    }

    [Fact]
    public async Task UpdateDetailsReturnsNotFoundWithoutSavingAsync()
    {
        var repository = new InMemoryWorkItemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UpdateWorkItemDetailsHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new UpdateWorkItemDetailsCommand(Guid.NewGuid(), "Title", "Description", "Actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("work_items.not_found", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task UpdateDetailsReturnsValidationFailureWithoutSavingAsync()
    {
        WorkItem workItem = CreateWorkItem();
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UpdateWorkItemDetailsHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new UpdateWorkItemDetailsCommand(workItem.Id, " ", "Description", "Actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task UpdateDetailsReturnsUnchangedWithoutSavingAsync()
    {
        WorkItem workItem = CreateWorkItem();
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UpdateWorkItemDetailsHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new UpdateWorkItemDetailsCommand(
                workItem.Id,
                workItem.Title,
                workItem.Description,
                "Actor"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Changed);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task UnassignRemovesAssigneeAndSavesExactlyOnceAsync()
    {
        WorkItem workItem = CreateWorkItem();
        workItem.Assign("assignee", "actor", _currentTime.AddMinutes(-1));
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UnassignWorkItemHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new UnassignWorkItemCommand(workItem.Id, " analyst "),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Changed);
        Assert.Null(workItem.AssigneeIdentifier);
        Assert.Equal(1, unitOfWork.SaveCallCount);
        Assert.Equal(cancellationToken, repository.GetByIdCancellationToken);
        Assert.Equal(cancellationToken, unitOfWork.SaveCancellationToken);
    }

    [Fact]
    public async Task UnassignReturnsNotFoundWithoutSavingAsync()
    {
        var repository = new InMemoryWorkItemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UnassignWorkItemHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new UnassignWorkItemCommand(Guid.NewGuid(), "Actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("work_items.not_found", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task UnassignReturnsValidationFailureWithoutSavingAsync()
    {
        WorkItem workItem = CreateWorkItem();
        workItem.Assign("assignee", "actor", _currentTime.AddMinutes(-1));
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UnassignWorkItemHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new UnassignWorkItemCommand(workItem.Id, " "),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task UnassignReturnsBusinessRuleForTerminalStateWithoutSavingAsync()
    {
        WorkItem workItem = CreateCancelledWorkItem();
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UnassignWorkItemHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new UnassignWorkItemCommand(workItem.Id, "Actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("work_items.unassignment_forbidden", result.Error?.Code);
        Assert.Equal(ApplicationErrorType.BusinessRule, result.Error?.Type);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task UnassignReturnsUnchangedWithoutSavingAsync()
    {
        WorkItem workItem = CreateWorkItem();
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UnassignWorkItemHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new UnassignWorkItemCommand(workItem.Id, "Actor"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Changed);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ChangePriorityUpdatesAndSavesExactlyOnceAsync()
    {
        WorkItem workItem = CreateWorkItem();
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new ChangeWorkItemPriorityHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeWorkItemPriorityCommand(
                workItem.Id,
                WorkItemPriority.Critical,
                " analyst "),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Changed);
        Assert.Equal(WorkItemPriority.Critical, workItem.Priority);
        Assert.Equal(1, unitOfWork.SaveCallCount);
        Assert.Equal(cancellationToken, repository.GetByIdCancellationToken);
        Assert.Equal(cancellationToken, unitOfWork.SaveCancellationToken);
    }

    [Fact]
    public async Task ChangePriorityReturnsNotFoundWithoutSavingAsync()
    {
        var repository = new InMemoryWorkItemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new ChangeWorkItemPriorityHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeWorkItemPriorityCommand(
                Guid.NewGuid(),
                WorkItemPriority.Low,
                "Actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("work_items.not_found", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ChangePriorityReturnsValidationFailureWithoutSavingAsync()
    {
        WorkItem workItem = CreateWorkItem();
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new ChangeWorkItemPriorityHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeWorkItemPriorityCommand(workItem.Id, (WorkItemPriority)999, "Actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ChangePriorityReturnsBusinessRuleForTerminalStateWithoutSavingAsync()
    {
        WorkItem workItem = CreateCancelledWorkItem();
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new ChangeWorkItemPriorityHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeWorkItemPriorityCommand(
                workItem.Id,
                WorkItemPriority.Critical,
                "Actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("work_items.priority_change_forbidden", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ChangePriorityReturnsUnchangedWithoutSavingAsync()
    {
        WorkItem workItem = CreateWorkItem();
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new ChangeWorkItemPriorityHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeWorkItemPriorityCommand(workItem.Id, workItem.Priority, "Actor"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Changed);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ChangeDueDateUpdatesAndSavesExactlyOnceAsync()
    {
        WorkItem workItem = CreateWorkItem();
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new ChangeWorkItemDueDateHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));
        DateTimeOffset newDueAt = _currentTime.AddDays(5);
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeWorkItemDueDateCommand(workItem.Id, newDueAt, " analyst "),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Changed);
        Assert.Equal(newDueAt, workItem.DueAtUtc);
        Assert.Equal(1, unitOfWork.SaveCallCount);
        Assert.Equal(cancellationToken, repository.GetByIdCancellationToken);
        Assert.Equal(cancellationToken, unitOfWork.SaveCancellationToken);
    }

    [Fact]
    public async Task ChangeDueDateReturnsNotFoundWithoutSavingAsync()
    {
        var repository = new InMemoryWorkItemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new ChangeWorkItemDueDateHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeWorkItemDueDateCommand(Guid.NewGuid(), null, "Actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("work_items.not_found", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ChangeDueDateReturnsValidationFailureWithoutSavingAsync()
    {
        WorkItem workItem = CreateWorkItem();
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new ChangeWorkItemDueDateHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeWorkItemDueDateCommand(workItem.Id, workItem.CreatedAtUtc, "Actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ChangeDueDateReturnsBusinessRuleForTerminalStateWithoutSavingAsync()
    {
        WorkItem workItem = CreateCancelledWorkItem();
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new ChangeWorkItemDueDateHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeWorkItemDueDateCommand(workItem.Id, null, "Actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("work_items.due_date_change_forbidden", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ChangeDueDateReturnsUnchangedWithoutSavingAsync()
    {
        WorkItem workItem = CreateWorkItem();
        InMemoryWorkItemRepository repository = SeededRepository(workItem);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new ChangeWorkItemDueDateHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeWorkItemDueDateCommand(workItem.Id, workItem.DueAtUtc, "Actor"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Changed);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    private static InMemoryWorkItemRepository SeededRepository(WorkItem workItem)
    {
        var repository = new InMemoryWorkItemRepository();
        repository.Seed(workItem);
        return repository;
    }

    private static WorkItem CreateWorkItem()
    {
        return WorkItem.Create(
            Guid.NewGuid(),
            WorkItemType.Incident,
            "Title",
            "Description",
            WorkItemPriority.High,
            _currentTime.AddDays(2),
            "Creator",
            _currentTime.AddDays(-2));
    }

    private static WorkItem CreateCancelledWorkItem()
    {
        WorkItem workItem = CreateWorkItem();
        workItem.TransitionTo(
            WorkItemStatus.Cancelled,
            "Actor",
            _currentTime.AddMinutes(-1),
            "Cancelled for test");
        return workItem;
    }
}
