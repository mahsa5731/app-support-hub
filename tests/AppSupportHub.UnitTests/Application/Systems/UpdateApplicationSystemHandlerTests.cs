using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.UpdateApplicationSystem;
using AppSupportHub.Domain.Systems;
using AppSupportHub.UnitTests.TestDoubles;

namespace AppSupportHub.UnitTests.Application.Systems;

public sealed class UpdateApplicationSystemHandlerTests
{
    private static readonly DateTimeOffset _currentTime =
        new(2026, 4, 9, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsyncUpdatesMetadataAndSavesExactlyOnceAsync()
    {
        ApplicationSystem system = CreateSystem("Payroll");
        var repository = new InMemoryApplicationSystemRepository();
        repository.Seed(system);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UpdateApplicationSystemHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            CreateCommand(system.Id) with { Name = " Payroll Platform " },
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Changed);
        Assert.Equal("Payroll Platform", system.Name);
        Assert.Equal(_currentTime, system.UpdatedAtUtc);
        Assert.Equal(system.Id, repository.ExcludedApplicationSystemId);
        Assert.Equal(1, unitOfWork.SaveCallCount);
        Assert.Equal(cancellationToken, repository.GetByIdCancellationToken);
        Assert.Equal(cancellationToken, repository.NameExistsCancellationToken);
        Assert.Equal(cancellationToken, unitOfWork.SaveCancellationToken);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsNotFoundWithoutCheckingNameOrSavingAsync()
    {
        var repository = new InMemoryApplicationSystemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UpdateApplicationSystemHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            CreateCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("systems.not_found", result.Error?.Code);
        Assert.Equal(0, repository.NameExistsCallCount);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsNameConflictExcludingCurrentSystemAsync()
    {
        ApplicationSystem current = CreateSystem("Payroll");
        var repository = new InMemoryApplicationSystemRepository();
        repository.Seed(current);
        repository.Seed(CreateSystem("Finance"));
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UpdateApplicationSystemHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            CreateCommand(current.Id) with { Name = " finance " },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("systems.name_conflict", result.Error?.Code);
        Assert.Equal(current.Id, repository.ExcludedApplicationSystemId);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsValidationFailureWithoutSavingAsync()
    {
        ApplicationSystem system = CreateSystem("Payroll");
        var repository = new InMemoryApplicationSystemRepository();
        repository.Seed(system);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UpdateApplicationSystemHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            CreateCommand(system.Id) with { Description = "   " },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsUnchangedWithoutSavingAsync()
    {
        ApplicationSystem system = CreateSystem("Payroll");
        var repository = new InMemoryApplicationSystemRepository();
        repository.Seed(system);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UpdateApplicationSystemHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));

        ApplicationResult<MutationOutcome> result = await handler.ExecuteAsync(
            CreateCommand(system.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Changed);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    private static UpdateApplicationSystemCommand CreateCommand(Guid id)
    {
        return new UpdateApplicationSystemCommand(
            id,
            "Payroll",
            "Description",
            ApplicationSystemType.Custom,
            ApplicationCriticality.High,
            "Business owner",
            "Technical owner",
            "Support team",
            null);
    }

    private static ApplicationSystem CreateSystem(string name)
    {
        return ApplicationSystem.Create(
            name,
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
