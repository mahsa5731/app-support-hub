using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Domain.Systems;

namespace AppSupportHub.Application.Systems.CreateApplicationSystem;

public sealed class CreateApplicationSystemHandler
{
    private readonly IApplicationSystemRepository _applicationSystemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateApplicationSystemHandler(
        IApplicationSystemRepository applicationSystemRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(applicationSystemRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _applicationSystemRepository = applicationSystemRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<ApplicationResult<CreatedApplicationSystem>> ExecuteAsync(
        CreateApplicationSystemCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string name = command.Name ?? string.Empty;
        string normalizedName = name.Trim();
        bool nameExists = await _applicationSystemRepository.NameExistsAsync(
            normalizedName,
            cancellationToken);

        if (nameExists)
        {
            return ApplicationResultFactory.Failure<CreatedApplicationSystem>(new ApplicationError(
                "systems.name_conflict",
                "An application system with this name already exists.",
                ApplicationErrorType.Conflict));
        }

        ApplicationSystem applicationSystem;

        try
        {
            applicationSystem = ApplicationSystem.Create(
                name,
                command.Description,
                command.Type,
                command.Criticality,
                command.InitialLifecycleStatus,
                command.BusinessOwner,
                command.TechnicalOwner,
                command.SupportTeam,
                command.VendorName,
                _timeProvider.GetUtcNow());
        }
        catch (ArgumentException exception)
        {
            return ApplicationResultFactory.Failure<CreatedApplicationSystem>(new ApplicationError(
                "validation.invalid_input",
                exception.Message,
                ApplicationErrorType.Validation));
        }

        await _applicationSystemRepository.AddAsync(applicationSystem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApplicationResultFactory.Success(
            new CreatedApplicationSystem(applicationSystem.Id));
    }
}
