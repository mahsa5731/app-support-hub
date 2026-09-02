using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSupportHub.Web.Security;

public sealed class CurrentActor(IHttpContextAccessor httpContextAccessor)
{
    public string GetRequiredUsername()
    {
        string? username = httpContextAccessor.HttpContext?.User.Identity?.Name;
        return string.IsNullOrWhiteSpace(username)
            ? throw new InvalidOperationException("An authenticated actor is required.")
            : username;
    }
}

public static class PageMutationAuthorization
{
    public static async Task<IActionResult?> AuthorizeMutationAsync(
        this PageModel pageModel,
        IAuthorizationService authorizationService,
        string policy)
    {
        AuthorizationResult result = await authorizationService.AuthorizeAsync(
            pageModel.User,
            resource: null,
            policy);
        if (result.Succeeded)
        {
            return null;
        }

        return pageModel.User.Identity?.IsAuthenticated == true
            ? pageModel.Forbid()
            : pageModel.Challenge();
    }
}
