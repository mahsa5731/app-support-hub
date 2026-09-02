using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AppSupportHub.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSupportHub.Web.Pages.Account;

public sealed class LoginModel(
    PortfolioAccounts accounts,
    ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public bool InteractiveLoginEnabled => accounts.InteractiveLoginEnabled;

    public void OnGet()
    {
        ReturnUrl = SafeReturnUrl(ReturnUrl);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = SafeReturnUrl(ReturnUrl);
        string password = Input.Password;
        bool inputIsValid = ModelState.IsValid;
        ModelState.Remove("Input.Password");
        Input.Password = string.Empty;
        if (!accounts.InteractiveLoginEnabled)
        {
            return Page();
        }

        PortfolioAccount? account = inputIsValid
            ? accounts.Authenticate(Input.Username, password)
            : null;
        if (account is null)
        {
            SecurityLog.LoginFailed(logger);
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, account.Username),
                new Claim(ClaimTypes.Role, account.Role),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = false });
        SecurityLog.LoginSucceeded(logger, account.Username, account.Role);
        return ReturnUrl is null ? RedirectToPage("/Index") : LocalRedirect(ReturnUrl);
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        string username = User.Identity?.Name ?? "anonymous";
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        SecurityLog.LogoutSucceeded(logger, username);
        TempData["StatusMessage"] = "You have signed out.";
        return RedirectToPage("/Index");
    }

    private string? SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;

    public sealed class LoginInput
    {
        [Required, StringLength(200)]
        public string Username { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
