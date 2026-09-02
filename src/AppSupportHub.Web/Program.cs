using AppSupportHub.Web;
using AppSupportHub.Web.Api.V1;
using AppSupportHub.Web.DemoData;
using AppSupportHub.Web.Operations;
using AppSupportHub.Web.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
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

await app.SeedDemoDataAsync();
await app.RunAsync();

public partial class Program;
