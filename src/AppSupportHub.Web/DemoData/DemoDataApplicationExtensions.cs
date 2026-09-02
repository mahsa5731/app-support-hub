namespace AppSupportHub.Web.DemoData;

public static class DemoDataApplicationExtensions
{
    public static async Task SeedDemoDataAsync(this WebApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (!application.Environment.IsDevelopment()
            || !application.Configuration.GetValue<bool>("AppSupportHub:SeedDemoData"))
        {
            return;
        }

        await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
        DemoDataSeeder seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync(application.Lifetime.ApplicationStopping);
    }
}
