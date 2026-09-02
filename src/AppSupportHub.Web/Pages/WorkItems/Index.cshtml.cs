using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.WorkItems.Inputs;
using AppSupportHub.Application.WorkItems.ListWorkItems;
using AppSupportHub.Application.WorkItems.ReadModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSupportHub.Web.Pages.WorkItems;

public sealed class IndexModel(
    WorkItemInputFactory inputFactory,
    ListWorkItemsHandler handler) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid? ApplicationSystemId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? TitleSearch { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Type { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Priority { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? AssigneeIdentifier { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OverdueOnly { get; set; }

    public IReadOnlyList<string> Types => inputFactory.Types;

    public IReadOnlyList<string> Priorities => inputFactory.Priorities;

    public IReadOnlyList<string> Statuses => inputFactory.Statuses;

    public IReadOnlyList<WorkItemSummary> WorkItems { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ApplicationResult<ListWorkItemsQuery> parsed = inputFactory.CreateListQuery(
            ApplicationSystemId,
            TitleSearch,
            Type,
            Priority,
            Status,
            AssigneeIdentifier,
            OverdueOnly,
            50);
        if (!parsed.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, parsed.Error!.Description);
            return;
        }

        ApplicationResult<IReadOnlyList<WorkItemSummary>> result =
            await handler.ExecuteAsync(parsed.Value, cancellationToken);
        if (result.IsSuccess)
        {
            WorkItems = result.Value;
        }
        else
        {
            ModelState.AddModelError(string.Empty, result.Error!.Description);
        }
    }
}
