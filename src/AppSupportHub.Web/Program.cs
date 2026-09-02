using AppSupportHub.Web;
using AppSupportHub.Web.Api.V1;
using AppSupportHub.Web.DemoData;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string? connectionString = builder.Configuration.GetConnectionString("AppSupportHub");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:AppSupportHub is required for business workflows. "
        + "Set ConnectionStrings__AppSupportHub before starting AppSupportHub.Web.");
}

builder.Services.AddWebApplication(connectionString);
WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapOpenApi();
app.MapApiV1();
app.MapRazorPages().WithStaticAssets();

await app.SeedDemoDataAsync();
await app.RunAsync();

public partial class Program;
