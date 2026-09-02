using System.Globalization;
using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.WorkItems.AssignWorkItem;
using AppSupportHub.Application.WorkItems.ChangeWorkItemDueDate;
using AppSupportHub.Application.WorkItems.ChangeWorkItemPriority;
using AppSupportHub.Application.WorkItems.GetWorkItem;
using AppSupportHub.Application.WorkItems.Inputs;
using AppSupportHub.Application.WorkItems.ReadModels;
using AppSupportHub.Application.WorkItems.TransitionWorkItemStatus;
using AppSupportHub.Application.WorkItems.UnassignWorkItem;
using AppSupportHub.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSupportHub.Web.Pages.WorkItems;

public sealed class DetailsModel(
    WorkItemInputFactory inputFactory,
    GetWorkItemHandler getHandler,
    AssignWorkItemHandler assignHandler,
    UnassignWorkItemHandler unassignHandler,
    ChangeWorkItemPriorityHandler priorityHandler,
    ChangeWorkItemDueDateHandler dueDateHandler,
    TransitionWorkItemStatusHandler transitionHandler) : PageModel
{
    [BindProperty]
    public string AssigneeIdentifier { get; set; } = string.Empty;

    [BindProperty]
    public string Priority { get; set; } = string.Empty;

    [BindProperty]
    public string? DueAtUtc { get; set; }

    [BindProperty]
    public string TargetStatus { get; set; } = string.Empty;

    [BindProperty]
    public string? Comment { get; set; }

    [BindProperty]
    public string? ResolutionSummary { get; set; }

    public WorkItemDetail WorkItem { get; private set; } = null!;

    public IReadOnlyList<string> Priorities => inputFactory.Priorities;

    public IReadOnlyList<string> Statuses => inputFactory.Statuses;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await LoadAsync(id, true, cancellationToken);
    }

    public async Task<IActionResult> OnPostAssignAsync(Guid id, CancellationToken cancellationToken)
    {
        ApplicationResult<MutationOutcome> result = await assignHandler.ExecuteAsync(
            new AssignWorkItemCommand(id, AssigneeIdentifier, DemoActor.Identifier),
            cancellationToken);
        return await CompleteMutationAsync(id, result, "Assignment updated.", cancellationToken);
    }

    public async Task<IActionResult> OnPostUnassignAsync(Guid id, CancellationToken cancellationToken)
    {
        ApplicationResult<MutationOutcome> result = await unassignHandler.ExecuteAsync(
            new UnassignWorkItemCommand(id, DemoActor.Identifier),
            cancellationToken);
        return await CompleteMutationAsync(id, result, "Assignment removed.", cancellationToken);
    }

    public async Task<IActionResult> OnPostPriorityAsync(Guid id, CancellationToken cancellationToken)
    {
        ApplicationResult<ChangeWorkItemPriorityCommand> parsed =
            inputFactory.CreatePriorityCommand(id, Priority, DemoActor.Identifier);
        if (!parsed.IsSuccess)
        {
            await LoadAsync(id, false, cancellationToken);
            return ApplicationErrorMapper.ToPageResult(this, parsed.Error!, nameof(Priority));
        }

        ApplicationResult<MutationOutcome> result = await priorityHandler.ExecuteAsync(
            parsed.Value,
            cancellationToken);
        return await CompleteMutationAsync(id, result, "Priority updated.", cancellationToken);
    }

    public async Task<IActionResult> OnPostDueDateAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!UtcInputParser.TryParseDateTimeLocalUtc(DueAtUtc, out DateTimeOffset? dueAt))
        {
            ModelState.AddModelError(nameof(DueAtUtc), "Enter an unambiguous UTC date and time.");
            await LoadAsync(id, false, cancellationToken);
            return Page();
        }

        ApplicationResult<MutationOutcome> result = await dueDateHandler.ExecuteAsync(
            new ChangeWorkItemDueDateCommand(id, dueAt, DemoActor.Identifier),
            cancellationToken);
        return await CompleteMutationAsync(id, result, "Due date updated.", cancellationToken);
    }

    public async Task<IActionResult> OnPostTransitionAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        ApplicationResult<TransitionWorkItemStatusCommand> parsed =
            inputFactory.CreateTransitionCommand(
                id,
                TargetStatus,
                DemoActor.Identifier,
                Comment,
                ResolutionSummary);
        if (!parsed.IsSuccess)
        {
            await LoadAsync(id, false, cancellationToken);
            return ApplicationErrorMapper.ToPageResult(
                this,
                parsed.Error!,
                nameof(TargetStatus));
        }

        ApplicationResult<MutationOutcome> result = await transitionHandler.ExecuteAsync(
            parsed.Value,
            cancellationToken);
        return await CompleteMutationAsync(id, result, "Status transitioned.", cancellationToken);
    }

    private async Task<IActionResult> CompleteMutationAsync(
        Guid id,
        ApplicationResult<MutationOutcome> result,
        string message,
        CancellationToken cancellationToken)
    {
        if (!result.IsSuccess)
        {
            await LoadAsync(id, false, cancellationToken);
            return ApplicationErrorMapper.ToPageResult(this, result.Error!);
        }

        TempData["StatusMessage"] = result.Value.Changed
            ? message
            : "No change was needed.";
        return RedirectToPage("Details", new { id });
    }

    private async Task<IActionResult> LoadAsync(
        Guid id,
        bool initializeInputs,
        CancellationToken cancellationToken)
    {
        ApplicationResult<WorkItemDetail> result = await getHandler.ExecuteAsync(
            new GetWorkItemQuery(id),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ApplicationErrorMapper.ToPageResult(this, result.Error!);
        }

        WorkItem = result.Value;
        if (initializeInputs)
        {
            AssigneeIdentifier = WorkItem.AssigneeIdentifier ?? string.Empty;
            Priority = WorkItem.PriorityName;
            DueAtUtc = WorkItem.DueAtUtc?.ToString(
                "yyyy-MM-dd'T'HH:mm",
                CultureInfo.InvariantCulture);
            TargetStatus = WorkItem.StatusName;
        }

        return Page();
    }
}
