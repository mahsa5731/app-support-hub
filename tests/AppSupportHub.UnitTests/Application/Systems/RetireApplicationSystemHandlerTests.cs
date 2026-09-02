using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.RetireApplicationSystem;
using AppSupportHub.Domain.Systems;
using AppSupportHub.UnitTests.TestDoubles;

namespace AppSupportHub.UnitTests.Application.Systems;

public sealed class RetireApplicationSystemHandlerTests
{
    private static readonly DateTimeOffset _currentTime = new(
        2026,
        2,
        5,
        16,
        0,
        0,
        TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsyncReturnsNotFoundWithoutSavingAsync()
    {
        var repository = new InMemoryApplicationSystemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        RetireApplicationSystemHandler handler = CreateHandler(repository, unitOfWork);
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new RetireApplicationSystemCommand(Guid.NewGuid(), "Reason"),
            cancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("systems.not_found", result.Error?.Code);
        Assert.Equal(ApplicationErrorType.NotFound, result.Error?.Type);
        Assert.Equal(0, unitOfWork.SaveCallCount);
        Assert.Equal(cancellationToken, repository.GetByIdCancellationToken);
    }

    [Fact]
    public async Task ExecuteAsyncRetiresSystemAndSavesExactlyOnceAsync()
    {
        ApplicationSystem system = CreateDomainSystem();
        var repository = new InMemoryApplicationSystemRepository();
        repository.Seed(system);
        var unitOfWork = new RecordingUnitOfWork();
        RetireApplicationSystemHandler handler = CreateHandler(repository, unitOfWork);
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new RetireApplicationSystemCommand(system.Id, " No longer used "),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Changed);
        Assert.True(system.IsRetired);
        Assert.Equal("No longer used", system.RetirementReason);
        Assert.Equal(_currentTime, system.RetiredAtUtc);
        Assert.Equal(1, unitOfWork.SaveCallCount);
        Assert.Equal(cancellationToken, unitOfWork.SaveCancellationToken);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsValidationFailureWithoutSavingAsync()
    {
        ApplicationSystem system = CreateDomainSystem();
        var repository = new InMemoryApplicationSystemRepository();
        repository.Seed(system);
        var unitOfWork = new RecordingUnitOfWork();
        RetireApplicationSystemHandler handler = CreateHandler(repository, unitOfWork);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new RetireApplicationSystemCommand(system.Id, "   "),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
        Assert.False(system.IsRetired);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsUnchangedWithoutSavingWhenAlreadyRetiredAsync()
    {
        ApplicationSystem system = CreateDomainSystem();
        system.TransitionLifecycle(
            ApplicationLifecycleStatus.Retired,
            _currentTime.AddMinutes(-1),
            "Earlier retirement");
        var repository = new InMemoryApplicationSystemRepository();
        repository.Seed(system);
        var unitOfWork = new RecordingUnitOfWork();
        RetireApplicationSystemHandler handler = CreateHandler(repository, unitOfWork);

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            new RetireApplicationSystemCommand(system.Id, "Different reason"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Changed);
        Assert.Equal("Earlier retirement", system.RetirementReason);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    private static RetireApplicationSystemHandler CreateHandler(
        InMemoryApplicationSystemRepository repository,
        RecordingUnitOfWork unitOfWork)
    {
        return new RetireApplicationSystemHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));
    }

    private static ApplicationSystem CreateDomainSystem()
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
