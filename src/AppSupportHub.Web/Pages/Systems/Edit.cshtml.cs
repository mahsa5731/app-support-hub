using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.GetApplicationSystem;
using AppSupportHub.Application.Systems.Inputs;
using AppSupportHub.Application.Systems.ReadModels;
using AppSupportHub.Application.Systems.UpdateApplicationSystem;
using AppSupportHub.Web.Http;
using AppSupportHub.Web.Models.Systems;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSupportHub.Web.Pages.Systems;

public sealed class EditModel(
    ApplicationSystemInputFactory inputFactory,
    GetApplicationSystemHandler getHandler,
    UpdateApplicationSystemHandler updateHandler) : PageModel
{
    [BindProperty]
    public SystemFormInput Input { get; set; } = new();

    public IReadOnlyList<string> Types => inputFactory.Types;

    public IReadOnlyList<string> Criticalities => inputFactory.Criticalities;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        ApplicationResult<ApplicationSystemDetail> result = await getHandler.ExecuteAsync(
            new GetApplicationSystemQuery(id),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ApplicationErrorMapper.ToPageResult(this, result.Error!);
        }

        ApplicationSystemDetail system = result.Value;
        Input = new SystemFormInput
        {
            Name = system.Name,
            Description = system.Description,
            Type = system.TypeName,
            Criticality = system.CriticalityName,
            BusinessOwner = system.BusinessOwner,
            TechnicalOwner = system.TechnicalOwner,
            SupportTeam = system.SupportTeam,
            VendorName = system.VendorName,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        ApplicationResult<UpdateApplicationSystemCommand> parsed =
            inputFactory.CreateUpdateCommand(
                id,
                Input.Name,
                Input.Description,
                Input.Type,
                Input.Criticality,
                Input.BusinessOwner,
                Input.TechnicalOwner,
                Input.SupportTeam,
                Input.VendorName);
        if (!parsed.IsSuccess)
        {
            return ApplicationErrorMapper.ToPageResult(this, parsed.Error!);
        }

        ApplicationResult<MutationOutcome> result = await updateHandler.ExecuteAsync(
            parsed.Value,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ApplicationErrorMapper.ToPageResult(this, result.Error!);
        }

        TempData["StatusMessage"] = result.Value.Changed
            ? "Application system updated."
            : "No application-system changes were needed.";
        return RedirectToPage("Details", new { id });
    }
}
