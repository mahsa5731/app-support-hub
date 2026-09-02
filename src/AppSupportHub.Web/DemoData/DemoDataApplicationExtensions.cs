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

        await SeedDemoDataCoreAsync(application);
    }

    public static async Task<bool> SeedFictionalDemoDataAsync(this WebApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (!application.Environment.IsProduction())
        {
            Console.Error.WriteLine("ASPNETCORE_ENVIRONMENT=Production is required.");
            return false;
        }

        if (!application.Configuration.GetValue<bool>("AppSupportHub:SeedDemoData"))
        {
            Console.Error.WriteLine("AppSupportHub:SeedDemoData=true is required.");
            return false;
        }

        await SeedDemoDataCoreAsync(application);
        return true;
    }

    private static async Task SeedDemoDataCoreAsync(WebApplication application)
    {
        await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
        DemoDataSeeder seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync(application.Lifetime.ApplicationStopping);
    }
}
