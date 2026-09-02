using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.Application.WorkItems.CreateWorkItem;

public sealed class CreateWorkItemHandler
{
    private readonly IApplicationSystemRepository _applicationSystemRepository;
    private readonly IWorkItemRepository _workItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateWorkItemHandler(
        IApplicationSystemRepository applicationSystemRepository,
        IWorkItemRepository workItemRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(applicationSystemRepository);
        ArgumentNullException.ThrowIfNull(workItemRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _applicationSystemRepository = applicationSystemRepository;
        _workItemRepository = workItemRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<ApplicationResult<CreatedWorkItem>> ExecuteAsync(
        CreateWorkItemCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        ApplicationSystem? applicationSystem = await _applicationSystemRepository.GetByIdAsync(
            command.ApplicationSystemId,
            cancellationToken);

        if (applicationSystem is null)
        {
            return ApplicationResultFactory.Failure<CreatedWorkItem>(new ApplicationError(
                "systems.not_found",
                "The application system was not found.",
                ApplicationErrorType.NotFound));
        }

        if (applicationSystem.IsRetired)
        {
            return ApplicationResultFactory.Failure<CreatedWorkItem>(new ApplicationError(
                "systems.retired",
                "Work items cannot be created for a retired application system.",
                ApplicationErrorType.BusinessRule));
        }

        WorkItem workItem;

        try
        {
            workItem = WorkItem.Create(
                command.ApplicationSystemId,
                command.Type,
                command.Title,
                command.Description,
                command.Priority,
                command.DueAt,
                command.ActorIdentifier,
                _timeProvider.GetUtcNow());
        }
        catch (ArgumentException exception)
        {
            return ApplicationResultFactory.Failure<CreatedWorkItem>(new ApplicationError(
                "validation.invalid_input",
                exception.Message,
                ApplicationErrorType.Validation));
        }

        await _workItemRepository.AddAsync(workItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApplicationResultFactory.Success(new CreatedWorkItem(workItem.Id));
    }
}
