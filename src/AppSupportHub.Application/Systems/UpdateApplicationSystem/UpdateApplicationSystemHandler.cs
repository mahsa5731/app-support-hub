using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Domain.Systems;

namespace AppSupportHub.Application.Systems.UpdateApplicationSystem;

public sealed class UpdateApplicationSystemHandler
{
    private readonly IApplicationSystemRepository _applicationSystemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public UpdateApplicationSystemHandler(
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

    public async Task<ApplicationResult<MutationOutcome>> ExecuteAsync(
        UpdateApplicationSystemCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        ApplicationSystem? applicationSystem = await _applicationSystemRepository.GetByIdAsync(
            command.ApplicationSystemId,
            cancellationToken);

        if (applicationSystem is null)
        {
            return ApplicationResultFactory.Failure<MutationOutcome>(new ApplicationError(
                "systems.not_found",
                "The application system was not found.",
                ApplicationErrorType.NotFound));
        }

        string normalizedName = (command.Name ?? string.Empty).Trim();
        bool nameExists = await _applicationSystemRepository.NameExistsAsync(
            normalizedName,
            command.ApplicationSystemId,
            cancellationToken);

        if (nameExists)
        {
            return ApplicationResultFactory.Failure<MutationOutcome>(new ApplicationError(
                "systems.name_conflict",
                "An application system with this name already exists.",
                ApplicationErrorType.Conflict));
        }

        bool changed;

        try
        {
            changed = applicationSystem.UpdateMetadata(
                command.Name ?? string.Empty,
                command.Description,
                command.Type,
                command.Criticality,
                command.BusinessOwner,
                command.TechnicalOwner,
                command.SupportTeam,
                command.VendorName,
                _timeProvider.GetUtcNow());
        }
        catch (ArgumentException exception)
        {
            return ApplicationResultFactory.Failure<MutationOutcome>(new ApplicationError(
                "validation.invalid_input",
                exception.Message,
                ApplicationErrorType.Validation));
        }

        if (changed)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApplicationResultFactory.Success(new MutationOutcome(changed));
    }
}
