using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.ChangeApplicationSystemLifecycle;
using AppSupportHub.Domain.Systems;
using AppSupportHub.UnitTests.TestDoubles;

namespace AppSupportHub.UnitTests.Application.Systems;

public sealed class ChangeApplicationSystemLifecycleHandlerTests
{
    private static readonly DateTimeOffset _currentTime =
        new(2026, 4, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsyncChangesLifecycleAndSavesExactlyOnceAsync()
    {
        ApplicationSystem system = CreateSystem(ApplicationLifecycleStatus.Active);
        var repository = new InMemoryApplicationSystemRepository();
        repository.Seed(system);
        var unitOfWork = new RecordingUnitOfWork();
        ChangeApplicationSystemLifecycleHandler handler = CreateHandler(repository, unitOfWork);
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeApplicationSystemLifecycleCommand(
                system.Id,
                ApplicationLifecycleStatus.Maintenance),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Changed);
        Assert.Equal(ApplicationLifecycleStatus.Maintenance, system.LifecycleStatus);
        Assert.Equal(1, unitOfWork.SaveCallCount);
        Assert.Equal(cancellationToken, repository.GetByIdCancellationToken);
        Assert.Equal(cancellationToken, unitOfWork.SaveCancellationToken);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsNotFoundWithoutSavingAsync()
    {
        var repository = new InMemoryApplicationSystemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        ChangeApplicationSystemLifecycleHandler handler = CreateHandler(repository, unitOfWork);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeApplicationSystemLifecycleCommand(
                Guid.NewGuid(),
                ApplicationLifecycleStatus.Active),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("systems.not_found", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsValidationFailureForUndefinedStatusAsync()
    {
        ApplicationSystem system = CreateSystem(ApplicationLifecycleStatus.Active);
        var repository = new InMemoryApplicationSystemRepository();
        repository.Seed(system);
        var unitOfWork = new RecordingUnitOfWork();
        ChangeApplicationSystemLifecycleHandler handler = CreateHandler(repository, unitOfWork);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeApplicationSystemLifecycleCommand(
                system.Id,
                (ApplicationLifecycleStatus)999),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsBusinessRuleForInvalidTransitionAsync()
    {
        ApplicationSystem system = CreateSystem(ApplicationLifecycleStatus.Planned);
        var repository = new InMemoryApplicationSystemRepository();
        repository.Seed(system);
        var unitOfWork = new RecordingUnitOfWork();
        ChangeApplicationSystemLifecycleHandler handler = CreateHandler(repository, unitOfWork);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeApplicationSystemLifecycleCommand(
                system.Id,
                ApplicationLifecycleStatus.Maintenance),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("systems.invalid_lifecycle_transition", result.Error?.Code);
        Assert.Equal(ApplicationErrorType.BusinessRule, result.Error?.Type);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsUnchangedWithoutSavingAsync()
    {
        ApplicationSystem system = CreateSystem(ApplicationLifecycleStatus.Active);
        var repository = new InMemoryApplicationSystemRepository();
        repository.Seed(system);
        var unitOfWork = new RecordingUnitOfWork();
        ChangeApplicationSystemLifecycleHandler handler = CreateHandler(repository, unitOfWork);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new ChangeApplicationSystemLifecycleCommand(
                system.Id,
                ApplicationLifecycleStatus.Active),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Changed);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    private static ChangeApplicationSystemLifecycleHandler CreateHandler(
        InMemoryApplicationSystemRepository repository,
        RecordingUnitOfWork unitOfWork)
    {
        return new ChangeApplicationSystemLifecycleHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));
    }

    private static ApplicationSystem CreateSystem(ApplicationLifecycleStatus lifecycleStatus)
    {
        return ApplicationSystem.Create(
            "System",
            "Description",
            ApplicationSystemType.Custom,
            ApplicationCriticality.High,
            lifecycleStatus,
            "Business owner",
            "Technical owner",
            "Support team",
            null,
            _currentTime.AddDays(-1));
    }
}
