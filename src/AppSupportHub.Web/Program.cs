using AppSupportHub.Web;
using AppSupportHub.Web.Api.V1;
using AppSupportHub.Web.DemoData;
using AppSupportHub.Web.Operations;
using AppSupportHub.Web.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

const string seedFictionalDemoDataArgument = "--seed-fictional-demo-data";
string[] builderArguments = args
    .Where(argument => !string.Equals(
        argument,
        seedFictionalDemoDataArgument,
        StringComparison.Ordinal))
    .ToArray();
bool seedFictionalDemoData = builderArguments.Length != args.Length;

WebApplicationBuilder builder = WebApplication.CreateBuilder(builderArguments);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
string? connectionString = builder.Configuration.GetConnectionString("AppSupportHub");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:AppSupportHub is required for business workflows. "
        + "Set ConnectionStrings__AppSupportHub before starting AppSupportHub.Web.");
}

builder.Services.AddWebApplication(connectionString);
WebApplication app = builder.Build();
_ = app.Services.GetRequiredService<PortfolioAccounts>();
app.UseMiddleware<RequestCorrelationMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UsePortfolioSecurityHeaders();
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
});
app.MapOpenApi();
app.MapApiV1();
app.MapRazorPages().WithStaticAssets();

if (seedFictionalDemoData)
{
    Environment.ExitCode = await app.SeedFictionalDemoDataAsync() ? 0 : 1;
    return;
}

await app.SeedDemoDataAsync();
await app.RunAsync();

public partial class Program;
