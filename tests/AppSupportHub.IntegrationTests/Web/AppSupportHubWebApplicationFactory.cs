using System.Net.Http.Json;
using System.Text.Json;
using AppSupportHub.Web.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AppSupportHub.IntegrationTests.Web;

internal sealed class AppSupportHubWebApplicationFactory(
    string connectionString,
    string environmentName = "Testing",
    bool seedDemoData = false,
    Action<IServiceCollection>? configureTestServices = null,
    bool interactiveLogin = true,
    string? automaticRole = SecurityRoles.Administrator)
    : WebApplicationFactory<Program>
{
    public const string AdministratorUsername = "phase06.administrator";
    public const string AnalystUsername = "phase06.analyst";

    public string AdministratorPassword { get; } = $"Test-{Guid.NewGuid():N}";

    public string AnalystPassword { get; } = $"Test-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environmentName);
        builder.UseSetting("ConnectionStrings:AppSupportHub", connectionString);
        builder.UseSetting("AppSupportHub:SeedDemoData", seedDemoData.ToString());
        builder.UseSetting(
            SecurityConfigurationKeys.EnableInteractiveLogin,
            interactiveLogin.ToString());
        if (interactiveLogin)
        {
            builder.UseSetting(SecurityConfigurationKeys.AnalystUsername, AnalystUsername);
            builder.UseSetting(SecurityConfigurationKeys.AnalystPassword, AnalystPassword);
            builder.UseSetting(
                SecurityConfigurationKeys.AdministratorUsername,
                AdministratorUsername);
            builder.UseSetting(
                SecurityConfigurationKeys.AdministratorPassword,
                AdministratorPassword);
        }

        if (configureTestServices is not null)
        {
            builder.ConfigureTestServices(configureTestServices);
        }
    }

    public HttpClient CreateHttpsClient(bool allowAutoRedirect = true)
    {
        HttpClient client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect,
            BaseAddress = new Uri("https://localhost", UriKind.Absolute),
        });
        if (automaticRole is not null)
        {
            Authenticate(client, automaticRole);
        }

        return client;
    }

    private void Authenticate(HttpClient client, string role)
    {
        string username = role == SecurityRoles.Analyst
            ? AnalystUsername
            : AdministratorUsername;
        string password = role == SecurityRoles.Analyst
            ? AnalystPassword
            : AdministratorPassword;
        string html = client.GetStringAsync("/Account/Login").GetAwaiter().GetResult();
        string token = AntiforgeryTokenExtractor.Extract(html);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Username"] = username,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = token,
        });
        using HttpResponseMessage response = client.PostAsync("/Account/Login", form)
            .GetAwaiter().GetResult();
        if ((int)response.StatusCode >= 400)
        {
            throw new InvalidOperationException("The automatic test login failed.");
        }

        JsonElement antiforgery = client.GetFromJsonAsync<JsonElement>(
            "/api/v1/security/antiforgery").GetAwaiter().GetResult();
        client.DefaultRequestHeaders.Add(
            antiforgery.GetProperty("headerName").GetString()!,
            antiforgery.GetProperty("requestToken").GetString()!);
    }
}
