using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.ChangeApplicationSystemLifecycle;
using AppSupportHub.Application.Systems.GetApplicationSystem;
using AppSupportHub.Application.Systems.Inputs;
using AppSupportHub.Application.Systems.ReadModels;
using AppSupportHub.Application.WorkItems.Inputs;
using AppSupportHub.Application.WorkItems.ListWorkItems;
using AppSupportHub.Application.WorkItems.ReadModels;
using AppSupportHub.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSupportHub.Web.Pages.Systems;

public sealed class DetailsModel(
    ApplicationSystemInputFactory systemInputFactory,
    WorkItemInputFactory workItemInputFactory,
    GetApplicationSystemHandler getHandler,
    ListWorkItemsHandler listWorkItemsHandler,
    ChangeApplicationSystemLifecycleHandler lifecycleHandler) : PageModel
{
    [BindProperty]
    public string TargetLifecycleStatus { get; set; } = string.Empty;

    [BindProperty]
    public string? RetirementReason { get; set; }

    [BindProperty]
    public bool ConfirmLifecycleChange { get; set; }

    public ApplicationSystemDetail System { get; private set; } = null!;

    public IReadOnlyList<WorkItemSummary> RelatedWorkItems { get; private set; } = [];

    public IReadOnlyList<string> LifecycleStatuses => systemInputFactory.LifecycleStatuses;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await LoadAsync(id, cancellationToken);
    }

    public async Task<IActionResult> OnPostLifecycleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!ConfirmLifecycleChange)
        {
            ModelState.AddModelError(
                nameof(ConfirmLifecycleChange),
                "Confirm the lifecycle change before continuing.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(id, cancellationToken);
            return Page();
        }

        ApplicationResult<ChangeApplicationSystemLifecycleCommand> parsed =
            systemInputFactory.CreateLifecycleCommand(
                id,
                TargetLifecycleStatus,
                RetirementReason);
        if (!parsed.IsSuccess)
        {
            await LoadAsync(id, cancellationToken);
            return ApplicationErrorMapper.ToPageResult(this, parsed.Error!);
        }

        ApplicationResult<MutationOutcome> result = await lifecycleHandler.ExecuteAsync(
            parsed.Value,
            cancellationToken);
        if (!result.IsSuccess)
        {
            await LoadAsync(id, cancellationToken);
            return ApplicationErrorMapper.ToPageResult(this, result.Error!);
        }

        TempData["StatusMessage"] = result.Value.Changed
            ? "Application-system lifecycle updated."
            : "No lifecycle change was needed.";
        return RedirectToPage("Details", new { id });
    }

    private async Task<IActionResult> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        ApplicationResult<ApplicationSystemDetail> systemResult = await getHandler.ExecuteAsync(
            new GetApplicationSystemQuery(id),
            cancellationToken);
        if (!systemResult.IsSuccess)
        {
            return ApplicationErrorMapper.ToPageResult(this, systemResult.Error!);
        }

        System = systemResult.Value;
        if (string.IsNullOrWhiteSpace(TargetLifecycleStatus))
        {
            TargetLifecycleStatus = System.LifecycleStatusName;
        }

        ApplicationResult<ListWorkItemsQuery> parsed = workItemInputFactory.CreateListQuery(
            id,
            null,
            null,
            null,
            null,
            null,
            false,
            50);
        ApplicationResult<IReadOnlyList<WorkItemSummary>> workItemsResult =
            await listWorkItemsHandler.ExecuteAsync(parsed.Value, cancellationToken);
        if (workItemsResult.IsSuccess)
        {
            RelatedWorkItems = workItemsResult.Value;
        }
        else
        {
            ModelState.AddModelError(string.Empty, workItemsResult.Error!.Description);
        }

        return Page();
    }
}
