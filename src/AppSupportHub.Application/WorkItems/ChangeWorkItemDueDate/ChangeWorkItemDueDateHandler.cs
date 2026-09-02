using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.Application.WorkItems.ChangeWorkItemDueDate;

public sealed class ChangeWorkItemDueDateHandler
{
    private readonly IWorkItemRepository _workItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ChangeWorkItemDueDateHandler(
        IWorkItemRepository workItemRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(workItemRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _workItemRepository = workItemRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<ApplicationResult<MutationOutcome>> ExecuteAsync(
        ChangeWorkItemDueDateCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        WorkItem? workItem = await _workItemRepository.GetByIdAsync(
            command.WorkItemId,
            cancellationToken);

        if (workItem is null)
        {
            return ApplicationResultFactory.Failure<MutationOutcome>(NotFoundError());
        }

        bool changed;

        try
        {
            changed = workItem.ChangeDueDate(
                command.DueAt,
                command.ActorIdentifier,
                _timeProvider.GetUtcNow());
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
                "work_items.due_date_change_forbidden",
                exception.Message,
                ApplicationErrorType.BusinessRule));
        }

        if (changed)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApplicationResultFactory.Success(new MutationOutcome(changed));
    }

    private static ApplicationError NotFoundError()
    {
        return new ApplicationError(
            "work_items.not_found",
            "The work item was not found.",
            ApplicationErrorType.NotFound);
    }
}
