using AppSupportHub.Application.Operations;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSupportHub.Web.Pages;

public sealed class OperationsModel(GetOperationsOverviewHandler handler) : PageModel
{
    public OperationsOverview? Overview { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Overview = await handler.ExecuteAsync(cancellationToken);
    }
}
