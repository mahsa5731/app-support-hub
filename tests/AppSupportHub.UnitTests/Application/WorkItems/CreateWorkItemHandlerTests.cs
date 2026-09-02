using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.WorkItems.CreateWorkItem;
using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.UnitTests.TestDoubles;

namespace AppSupportHub.UnitTests.Application.WorkItems;

public sealed class CreateWorkItemHandlerTests
{
    private static readonly DateTimeOffset _currentTime = new(
        2026,
        2,
        6,
        10,
        0,
        0,
        TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsyncReturnsSystemNotFoundWithoutAddingOrSavingAsync()
    {
        var systemRepository = new InMemoryApplicationSystemRepository();
        var workItemRepository = new InMemoryWorkItemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        CreateWorkItemHandler handler = CreateHandler(
            systemRepository,
            workItemRepository,
            unitOfWork);
        CreateWorkItemCommand command = CreateCommand(Guid.NewGuid());

        ApplicationResult<CreatedWorkItem> result = await handler.ExecuteAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("systems.not_found", result.Error?.Code);
        Assert.Equal(ApplicationErrorType.NotFound, result.Error?.Type);
        Assert.Equal(0, workItemRepository.AddCallCount);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsyncRejectsRetiredSystemWithoutAddingOrSavingAsync()
    {
        ApplicationSystem system = CreateSystem();
        system.TransitionLifecycle(
            ApplicationLifecycleStatus.Retired,
            _currentTime.AddMinutes(-1),
            "Retired");
        var systemRepository = new InMemoryApplicationSystemRepository();
        systemRepository.Seed(system);
        var workItemRepository = new InMemoryWorkItemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        CreateWorkItemHandler handler = CreateHandler(
            systemRepository,
            workItemRepository,
            unitOfWork);

        ApplicationResult<CreatedWorkItem> result = await handler.ExecuteAsync(
            CreateCommand(system.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("systems.retired", result.Error?.Code);
        Assert.Equal(ApplicationErrorType.BusinessRule, result.Error?.Type);
        Assert.Equal(0, workItemRepository.AddCallCount);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsyncCreatesWorkItemAndSavesExactlyOnceAsync()
    {
        ApplicationSystem system = CreateSystem();
        var systemRepository = new InMemoryApplicationSystemRepository();
        systemRepository.Seed(system);
        var workItemRepository = new InMemoryWorkItemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        CreateWorkItemHandler handler = CreateHandler(
            systemRepository,
            workItemRepository,
            unitOfWork);
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<CreatedWorkItem> result = await handler.ExecuteAsync(
            CreateCommand(system.Id),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        WorkItem workItem = Assert.Single(workItemRepository.Items);
        Assert.Equal(result.Value.Id, workItem.Id);
        Assert.Equal(_currentTime, workItem.CreatedAtUtc);
        Assert.Equal(1, workItemRepository.AddCallCount);
        Assert.Equal(1, unitOfWork.SaveCallCount);
        Assert.Equal(cancellationToken, systemRepository.GetByIdCancellationToken);
        Assert.Equal(cancellationToken, workItemRepository.AddCancellationToken);
        Assert.Equal(cancellationToken, unitOfWork.SaveCancellationToken);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsValidationFailureWithoutAddingOrSavingAsync()
    {
        ApplicationSystem system = CreateSystem();
        var systemRepository = new InMemoryApplicationSystemRepository();
        systemRepository.Seed(system);
        var workItemRepository = new InMemoryWorkItemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        CreateWorkItemHandler handler = CreateHandler(
            systemRepository,
            workItemRepository,
            unitOfWork);
        CreateWorkItemCommand command = CreateCommand(system.Id) with { Title = "   " };

        ApplicationResult<CreatedWorkItem> result = await handler.ExecuteAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error?.Code);
        Assert.Equal(0, workItemRepository.AddCallCount);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    private static CreateWorkItemHandler CreateHandler(
        InMemoryApplicationSystemRepository systemRepository,
        InMemoryWorkItemRepository workItemRepository,
        RecordingUnitOfWork unitOfWork)
    {
        return new CreateWorkItemHandler(
            systemRepository,
            workItemRepository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));
    }

    private static CreateWorkItemCommand CreateCommand(Guid applicationSystemId)
    {
        return new CreateWorkItemCommand(
            applicationSystemId,
            WorkItemType.Incident,
            "Incident",
            "Description",
            WorkItemPriority.High,
            _currentTime.AddDays(1),
            "Creator");
    }

    private static ApplicationSystem CreateSystem()
    {
        return ApplicationSystem.Create(
            "Payroll",
            "Description",
            ApplicationSystemType.Custom,
            ApplicationCriticality.High,
            ApplicationLifecycleStatus.Active,
            "Business owner",
            "Technical owner",
            "Support team",
            null,
            _currentTime.AddDays(-1));
    }
}
