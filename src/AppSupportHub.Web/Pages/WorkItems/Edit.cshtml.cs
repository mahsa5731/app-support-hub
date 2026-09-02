using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.WorkItems.GetWorkItem;
using AppSupportHub.Application.WorkItems.ReadModels;
using AppSupportHub.Application.WorkItems.UpdateWorkItemDetails;
using AppSupportHub.Web.Http;
using AppSupportHub.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSupportHub.Web.Pages.WorkItems;

public sealed class EditModel(
    GetWorkItemHandler getHandler,
    UpdateWorkItemDetailsHandler updateHandler,
    CurrentActor currentActor) : PageModel
{
    [BindProperty]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        ApplicationResult<WorkItemDetail> result = await getHandler.ExecuteAsync(
            new GetWorkItemQuery(id),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ApplicationErrorMapper.ToPageResult(this, result.Error!);
        }

        Title = result.Value.Title;
        Description = result.Value.Description;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ModelState.AddModelError(nameof(Title), "The title field is required.");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ModelState.AddModelError(nameof(Description), "The description field is required.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        ApplicationResult<MutationOutcome> result = await updateHandler.ExecuteAsync(
            new UpdateWorkItemDetailsCommand(
                id,
                Title,
                Description,
                currentActor.GetRequiredUsername()),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ApplicationErrorMapper.ToPageResult(this, result.Error!);
        }

        TempData["StatusMessage"] = result.Value.Changed
            ? "Work-item details updated."
            : "No work-item detail changes were needed.";
        return RedirectToPage("Details", new { id });
    }
}
