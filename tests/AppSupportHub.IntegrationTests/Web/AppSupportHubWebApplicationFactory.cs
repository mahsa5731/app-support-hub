using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AppSupportHub.IntegrationTests.Web;

internal sealed class AppSupportHubWebApplicationFactory(
    string connectionString,
    string environmentName = "Testing",
    bool seedDemoData = false,
    Action<IServiceCollection>? configureTestServices = null)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environmentName);
        builder.UseSetting("ConnectionStrings:AppSupportHub", connectionString);
        builder.UseSetting("AppSupportHub:SeedDemoData", seedDemoData.ToString());

        if (configureTestServices is not null)
        {
            builder.ConfigureTestServices(configureTestServices);
        }
    }

    public HttpClient CreateHttpsClient(bool allowAutoRedirect = true)
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect,
            BaseAddress = new Uri("https://localhost", UriKind.Absolute),
        });
    }
}
