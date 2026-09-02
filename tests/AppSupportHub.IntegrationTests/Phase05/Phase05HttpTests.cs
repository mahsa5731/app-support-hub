using System.Net;
using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.Infrastructure.Persistence;
using AppSupportHub.Infrastructure.Persistence.Repositories;
using AppSupportHub.IntegrationTests.Persistence;
using AppSupportHub.IntegrationTests.Web;
using Microsoft.EntityFrameworkCore;

namespace AppSupportHub.IntegrationTests.Phase05;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class Phase05HttpTests(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public async Task AssessmentPrgAndCsvUploadRenderTheTwoPreviewOnlyJourneysAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem system = PostgreSqlTestData.CreateApplicationSystem(
            "Aurora Ledger Sandbox");
        WorkItem workItem = PostgreSqlTestData.CreateWorkItem(
            system.Id,
            WorkItemType.ChangeRequest);
        await using (AppSupportHubDbContext arrange = fixture.CreateDbContext())
        {
            await new ApplicationSystemRepository(arrange).AddAsync(system, CancellationToken.None);
            await new WorkItemRepository(arrange).AddAsync(workItem, CancellationToken.None);
            await arrange.SaveChangesAsync();
        }

        using var factory = new AppSupportHubWebApplicationFactory(fixture.ConnectionString);
        using HttpClient client = factory.CreateHttpsClient(allowAutoRedirect: false);
        string assessmentRoute = $"/WorkItems/{workItem.Id}/Assessment";
        string assessmentHtml = await client.GetStringAsync(assessmentRoute);
        string token = AntiforgeryTokenExtractor.Extract(assessmentHtml);
        using HttpResponseMessage saved = await client.PostAsync(
            assessmentRoute,
            Form(token, new Dictionary<string, string>
            {
                ["Input.BusinessNeed"] = "Synthetic business need",
                ["Input.TechnicalImpact"] = "Synthetic technical impact",
                ["Input.SecurityImpact"] = "Synthetic security impact",
                ["Input.Risk"] = "High",
                ["Input.AcceptanceCriteria"] = "Synthetic acceptance criteria",
                ["Input.TestPlan"] = "Synthetic test plan",
                ["Input.RollbackPlan"] = "Synthetic rollback plan",
            }));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.Equal(assessmentRoute, saved.Headers.Location!.OriginalString);
        string reloaded = await client.GetStringAsync(assessmentRoute);
        Assert.Contains("Synthetic business need", reloaded, StringComparison.Ordinal);
        Assert.Contains("demo.user@appsupporthub.local", reloaded, StringComparison.Ordinal);

        string previewHtml = await client.GetStringAsync("/LegacyImports");
        token = AntiforgeryTokenExtractor.Extract(previewHtml);
        string samplePath = Path.Combine(
            FindSolutionRoot(),
            "src", "AppSupportHub.Web", "wwwroot", "samples", "legacy-systems.csv");
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(token), "__RequestVerificationToken");
        var file = new ByteArrayContent(await File.ReadAllBytesAsync(samplePath));
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        multipart.Add(file, "Upload", "legacy-systems.csv");
        using HttpResponseMessage previewResponse = await client.PostAsync(
            "/LegacyImports",
            multipart);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        string preview = await previewResponse.Content.ReadAsStringAsync();
        Assert.Contains("✓ Ready: 1", preview, StringComparison.Ordinal);
        Assert.Contains("⚠ Review duplicate: 1", preview, StringComparison.Ordinal);
        Assert.Contains("✕ Reject: 1", preview, StringComparison.Ordinal);
        Assert.Contains("never imports or stores records", preview, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<caption>", preview, StringComparison.Ordinal);

        await using AppSupportHubDbContext verify = fixture.CreateDbContext();
        Assert.Equal(1, await verify.Set<ApplicationSystem>().CountAsync());
    }

    private static FormUrlEncodedContent Form(
        string token,
        IReadOnlyDictionary<string, string> values)
    {
        IEnumerable<KeyValuePair<string, string>> fields = values.Append(
            new KeyValuePair<string, string>("__RequestVerificationToken", token));
        return new FormUrlEncodedContent(fields);
    }

    private static string FindSolutionRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AppSupportHub.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
