using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.Inputs;
using AppSupportHub.Application.Systems.ListApplicationSystems;
using AppSupportHub.Application.Systems.ReadModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSupportHub.Web.Pages.Systems;

public sealed class IndexModel(
    ApplicationSystemInputFactory inputFactory,
    ListApplicationSystemsHandler handler) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Name { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Type { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Criticality { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? LifecycleStatus { get; set; }

    public IReadOnlyList<string> Types => inputFactory.Types;

    public IReadOnlyList<string> Criticalities => inputFactory.Criticalities;

    public IReadOnlyList<string> LifecycleStatuses => inputFactory.LifecycleStatuses;

    public IReadOnlyList<ApplicationSystemSummary> Systems { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ApplicationResult<ListApplicationSystemsQuery> parsed = inputFactory.CreateListQuery(
            Name,
            Type,
            Criticality,
            LifecycleStatus,
            50);
        if (!parsed.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, parsed.Error!.Description);
            return;
        }

        ApplicationResult<IReadOnlyList<ApplicationSystemSummary>> result =
            await handler.ExecuteAsync(parsed.Value, cancellationToken);
        if (result.IsSuccess)
        {
            Systems = result.Value;
        }
        else
        {
            ModelState.AddModelError(string.Empty, result.Error!.Description);
        }
    }
}
