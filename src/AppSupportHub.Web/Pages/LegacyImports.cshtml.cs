using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.LegacyImports;
using AppSupportHub.Web.Http;
using AppSupportHub.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSupportHub.Web.Pages;

public sealed class LegacyImportsModel(
    PreviewLegacyCsvHandler handler,
    IAuthorizationService authorizationService) : PageModel
{
    [BindProperty]
    public IFormFile? Upload { get; set; }

    public LegacyCsvPreview? Preview { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        IActionResult? denial = await this.AuthorizeMutationAsync(
            authorizationService,
            SecurityPolicies.AnalystOrAdministrator);
        if (denial is not null)
        {
            return denial;
        }

        if (Upload is null)
        {
            ModelState.AddModelError(nameof(Upload), "Choose a CSV file to preview.");
            return Page();
        }

        await using Stream content = Upload.OpenReadStream();
        ApplicationResult<LegacyCsvPreview> result = await handler.ExecuteAsync(
            new PreviewLegacyCsvCommand(
                content,
                Upload.Length,
                Path.GetExtension(Upload.FileName)),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ApplicationErrorMapper.ToPageResult(this, result.Error!, nameof(Upload));
        }

        Preview = result.Value;
        return Page();
    }
}
