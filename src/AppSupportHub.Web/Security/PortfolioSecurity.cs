using Microsoft.AspNetCore.Identity;

namespace AppSupportHub.Web.Security;

public static class SecurityRoles
{
    public const string Analyst = "Analyst";
    public const string Administrator = "Administrator";
}

public static class SecurityPolicies
{
    public const string AnalystOrAdministrator = "AnalystOrAdministrator";
    public const string AdministratorOnly = "AdministratorOnly";
    public const string UnsafeApiRateLimit = "UnsafeApi";
    public const string AntiforgeryHeader = "X-AppSupportHub-Antiforgery";
}

public static class SecurityConfigurationKeys
{
    public const string EnableInteractiveLogin =
        "AppSupportHub:Security:EnableInteractiveLogin";
    public const string AnalystUsername = "AppSupportHub:Security:Analyst:Username";
    public const string AnalystPassword = "AppSupportHub:Security:Analyst:Password";
    public const string AdministratorUsername =
        "AppSupportHub:Security:Administrator:Username";
    public const string AdministratorPassword =
        "AppSupportHub:Security:Administrator:Password";
}

public static partial class SecurityLog
{
    [LoggerMessage(6001, LogLevel.Information, "Portfolio login succeeded for {Username} with role {Role}.")]
    public static partial void LoginSucceeded(ILogger logger, string username, string role);

    [LoggerMessage(6002, LogLevel.Warning, "Portfolio login failed for a supplied username.")]
    public static partial void LoginFailed(ILogger logger);

    [LoggerMessage(6003, LogLevel.Warning, "Portfolio login rate limit rejected a remote client.")]
    public static partial void LoginRateLimited(ILogger logger);

    [LoggerMessage(6004, LogLevel.Information, "Portfolio logout succeeded for {Username}.")]
    public static partial void LogoutSucceeded(ILogger logger, string username);
}

public sealed record PortfolioAccount(string Username, string Role, string PasswordHash);

public sealed class PortfolioAccounts
{
    private readonly Dictionary<string, PortfolioAccount> _accounts;
    private readonly PasswordHasher<PortfolioAccount> _hasher = new();

    private PortfolioAccounts(bool interactiveLoginEnabled, IEnumerable<PortfolioAccount> accounts)
    {
        InteractiveLoginEnabled = interactiveLoginEnabled;
        _accounts = accounts.ToDictionary(account => account.Username, StringComparer.OrdinalIgnoreCase);
    }

    public bool InteractiveLoginEnabled { get; }

    public static PortfolioAccounts FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (!configuration.GetValue<bool>(SecurityConfigurationKeys.EnableInteractiveLogin))
        {
            return new PortfolioAccounts(false, []);
        }

        var hasher = new PasswordHasher<PortfolioAccount>();
        PortfolioAccount analyst = BuildAccount(
            configuration,
            SecurityConfigurationKeys.AnalystUsername,
            SecurityConfigurationKeys.AnalystPassword,
            SecurityRoles.Analyst,
            hasher);
        PortfolioAccount administrator = BuildAccount(
            configuration,
            SecurityConfigurationKeys.AdministratorUsername,
            SecurityConfigurationKeys.AdministratorPassword,
            SecurityRoles.Administrator,
            hasher);
        if (string.Equals(analyst.Username, administrator.Username, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Configuration keys '{SecurityConfigurationKeys.AnalystUsername}' and "
                + $"'{SecurityConfigurationKeys.AdministratorUsername}' must differ.");
        }

        return new PortfolioAccounts(true, [analyst, administrator]);
    }

    public PortfolioAccount? Authenticate(string? username, string? password)
    {
        if (!InteractiveLoginEnabled
            || string.IsNullOrWhiteSpace(username)
            || password is null
            || !_accounts.TryGetValue(username.Trim(), out PortfolioAccount? account))
        {
            return null;
        }

        PasswordVerificationResult result = _hasher.VerifyHashedPassword(
            account,
            account.PasswordHash,
            password);
        return result == PasswordVerificationResult.Failed ? null : account;
    }

    private static PortfolioAccount BuildAccount(
        IConfiguration configuration,
        string usernameKey,
        string passwordKey,
        string role,
        PasswordHasher<PortfolioAccount> hasher)
    {
        string? username = configuration[usernameKey];
        string? password = configuration[passwordKey];
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException($"Configuration key '{usernameKey}' is required.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            throw new InvalidOperationException(
                $"Configuration key '{passwordKey}' must contain at least 12 characters.");
        }

        var account = new PortfolioAccount(username.Trim(), role, string.Empty);
        return account with { PasswordHash = hasher.HashPassword(account, password) };
    }
}
