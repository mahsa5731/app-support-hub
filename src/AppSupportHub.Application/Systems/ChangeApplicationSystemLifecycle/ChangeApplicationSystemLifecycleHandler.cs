using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Domain.Systems;

namespace AppSupportHub.Application.Systems.ChangeApplicationSystemLifecycle;

public sealed class ChangeApplicationSystemLifecycleHandler
{
    private readonly IApplicationSystemRepository _applicationSystemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ChangeApplicationSystemLifecycleHandler(
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
        ChangeApplicationSystemLifecycleCommand command,
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

        bool changed;

        try
        {
            changed = applicationSystem.TransitionLifecycle(
                command.TargetLifecycleStatus,
                _timeProvider.GetUtcNow(),
                command.RetirementReason);
        }
        catch (ArgumentException exception)
        {
            return ApplicationResultFactory.Failure<MutationOutcome>(new ApplicationError(
                "validation.invalid_input",
                exception.Message,
                ApplicationErrorType.Validation));
        }
        catch (InvalidOperationException exception)
        {
            return ApplicationResultFactory.Failure<MutationOutcome>(new ApplicationError(
                "systems.invalid_lifecycle_transition",
                exception.Message,
                ApplicationErrorType.BusinessRule));
        }

        if (changed)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApplicationResultFactory.Success(new MutationOutcome(changed));
    }
}
