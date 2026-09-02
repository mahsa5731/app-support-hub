using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.CreateApplicationSystem;
using AppSupportHub.Domain.Systems;
using AppSupportHub.UnitTests.TestDoubles;

namespace AppSupportHub.UnitTests.Application.Systems;

public sealed class CreateApplicationSystemHandlerTests
{
    private static readonly DateTimeOffset _currentTime = new(
        2026,
        2,
        5,
        9,
        30,
        0,
        TimeSpan.FromHours(-6));

    [Fact]
    public async Task ExecuteAsyncCreatesSystemAndSavesExactlyOnceAsync()
    {
        var repository = new InMemoryApplicationSystemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var timeProvider = new FixedTimeProvider(_currentTime);
        var handler = new CreateApplicationSystemHandler(repository, unitOfWork, timeProvider);
        using var cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationSource.Token;

        ApplicationResult<CreatedApplicationSystem> result = await handler.ExecuteAsync(
            CreateCommand(),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        ApplicationSystem system = Assert.Single(repository.Items);
        Assert.Equal(result.Value.Id, system.Id);
        Assert.Equal(_currentTime.ToUniversalTime(), system.CreatedAtUtc);
        Assert.Equal(1, repository.NameExistsCallCount);
        Assert.Equal(1, repository.AddCallCount);
        Assert.Equal(1, unitOfWork.SaveCallCount);
        Assert.Equal(cancellationToken, repository.NameExistsCancellationToken);
        Assert.Equal(cancellationToken, repository.AddCancellationToken);
        Assert.Equal(cancellationToken, unitOfWork.SaveCancellationToken);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsCaseInsensitiveNameConflictWithoutAddingOrSavingAsync()
    {
        var repository = new InMemoryApplicationSystemRepository();
        repository.Seed(CreateDomainSystem("Payroll"));
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new CreateApplicationSystemHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));
        CreateApplicationSystemCommand command = CreateCommand() with { Name = " payroll " };

        ApplicationResult<CreatedApplicationSystem> result = await handler.ExecuteAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("systems.name_conflict", result.Error?.Code);
        Assert.Equal(ApplicationErrorType.Conflict, result.Error?.Type);
        Assert.Equal(0, repository.AddCallCount);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsValidationFailureWithoutAddingOrSavingAsync()
    {
        var repository = new InMemoryApplicationSystemRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new CreateApplicationSystemHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(_currentTime));
        CreateApplicationSystemCommand command = CreateCommand() with { Name = "   " };

        ApplicationResult<CreatedApplicationSystem> result = await handler.ExecuteAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid_input", result.Error?.Code);
        Assert.Equal(ApplicationErrorType.Validation, result.Error?.Type);
        Assert.Equal(0, repository.AddCallCount);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    private static CreateApplicationSystemCommand CreateCommand()
    {
        return new CreateApplicationSystemCommand(
            "Payroll",
            "Payroll application",
            ApplicationSystemType.Custom,
            ApplicationCriticality.High,
            ApplicationLifecycleStatus.Planned,
            "Finance",
            "Technology",
            "Business Applications",
            null);
    }

    private static ApplicationSystem CreateDomainSystem(string name)
    {
        return ApplicationSystem.Create(
            name,
            "Description",
            ApplicationSystemType.Custom,
            ApplicationCriticality.Medium,
            ApplicationLifecycleStatus.Active,
            "Business owner",
            "Technical owner",
            "Support team",
            null,
            _currentTime);
    }
}
