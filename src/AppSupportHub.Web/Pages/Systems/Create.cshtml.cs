using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.CreateApplicationSystem;
using AppSupportHub.Application.Systems.Inputs;
using AppSupportHub.Web.Http;
using AppSupportHub.Web.Models.Systems;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSupportHub.Web.Pages.Systems;

public sealed class CreateModel(
    ApplicationSystemInputFactory inputFactory,
    CreateApplicationSystemHandler handler) : PageModel
{
    [BindProperty]
    public SystemFormInput Input { get; set; } = new();

    [BindProperty]
    public string InitialLifecycleStatus { get; set; } = "Active";

    public IReadOnlyList<string> Types => inputFactory.Types;

    public IReadOnlyList<string> Criticalities => inputFactory.Criticalities;

    public IReadOnlyList<string> InitialLifecycleStatuses => inputFactory.InitialLifecycleStatuses;

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        ApplicationResult<CreateApplicationSystemCommand> parsed =
            inputFactory.CreateCreateCommand(
                Input.Name,
                Input.Description,
                Input.Type,
                Input.Criticality,
                InitialLifecycleStatus,
                Input.BusinessOwner,
                Input.TechnicalOwner,
                Input.SupportTeam,
                Input.VendorName);
        if (!parsed.IsSuccess)
        {
            return ApplicationErrorMapper.ToPageResult(this, parsed.Error!);
        }

        ApplicationResult<CreatedApplicationSystem> result = await handler.ExecuteAsync(
            parsed.Value,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ApplicationErrorMapper.ToPageResult(this, result.Error!);
        }

        TempData["StatusMessage"] = "Application system created.";
        return RedirectToPage("Details", new { id = result.Value.Id });
    }
}
