using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.Inputs;
using AppSupportHub.Application.Systems.ListApplicationSystems;
using AppSupportHub.Application.Systems.ReadModels;
using AppSupportHub.Application.WorkItems.CreateWorkItem;
using AppSupportHub.Application.WorkItems.Inputs;
using AppSupportHub.Web.Http;
using AppSupportHub.Web.Models.WorkItems;
using AppSupportHub.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSupportHub.Web.Pages.WorkItems;

public sealed class CreateModel(
    ApplicationSystemInputFactory systemInputFactory,
    WorkItemInputFactory workItemInputFactory,
    ListApplicationSystemsHandler listSystemsHandler,
    CreateWorkItemHandler createHandler,
    CurrentActor currentActor) : PageModel
{
    [BindProperty]
    public WorkItemFormInput Input { get; set; } = new();

    public IReadOnlyList<ApplicationSystemSummary> SelectableSystems { get; private set; } = [];

    public IReadOnlyList<string> Types => workItemInputFactory.Types;

    public IReadOnlyList<string> Priorities => workItemInputFactory.Priorities;

    public async Task<IActionResult> OnGetAsync(
        Guid? applicationSystemId,
        CancellationToken cancellationToken)
    {
        Input.ApplicationSystemId = applicationSystemId ?? Guid.Empty;
        await LoadSystemsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (Input.ApplicationSystemId == Guid.Empty)
        {
            ModelState.AddModelError(
                "Input.ApplicationSystemId",
                "Choose an application system.");
        }

        if (!UtcInputParser.TryParseDateTimeLocalUtc(
            Input.DueAtUtc,
            out DateTimeOffset? dueAt))
        {
            ModelState.AddModelError(
                "Input.DueAtUtc",
                "Enter an unambiguous UTC date and time.");
        }

        if (!ModelState.IsValid)
        {
            await LoadSystemsAsync(cancellationToken);
            return Page();
        }

        ApplicationResult<CreateWorkItemCommand> parsed =
            workItemInputFactory.CreateCreateCommand(
                Input.ApplicationSystemId,
                Input.Type,
                Input.Title,
                Input.Description,
                Input.Priority,
                dueAt,
                currentActor.GetRequiredUsername());
        if (!parsed.IsSuccess)
        {
            await LoadSystemsAsync(cancellationToken);
            return ApplicationErrorMapper.ToPageResult(this, parsed.Error!);
        }

        ApplicationResult<CreatedWorkItem> result = await createHandler.ExecuteAsync(
            parsed.Value,
            cancellationToken);
        if (!result.IsSuccess)
        {
            await LoadSystemsAsync(cancellationToken);
            return ApplicationErrorMapper.ToPageResult(this, result.Error!);
        }

        TempData["StatusMessage"] = "Work item created.";
        return RedirectToPage("Details", new { id = result.Value.Id });
    }

    private async Task LoadSystemsAsync(CancellationToken cancellationToken)
    {
        ApplicationResult<ListApplicationSystemsQuery> parsed =
            systemInputFactory.CreateListQuery(null, null, null, null, 100);
        ApplicationResult<IReadOnlyList<ApplicationSystemSummary>> result =
            await listSystemsHandler.ExecuteAsync(parsed.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!.Description);
            return;
        }

        SelectableSystems = result.Value
            .Where(system => system.LifecycleStatusName != "Retired")
            .ToArray();
    }
}
