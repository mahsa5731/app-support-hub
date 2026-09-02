using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AppSupportHub.Domain.ChangeAssessments;
using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.Infrastructure.Persistence;
using AppSupportHub.Infrastructure.Persistence.Repositories;
using AppSupportHub.IntegrationTests.Persistence;
using AppSupportHub.IntegrationTests.Web;
using AppSupportHub.Web.Security;

namespace AppSupportHub.IntegrationTests.Phase06;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class Phase06SecurityTests(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public async Task AnonymousDemoReadsStayPublicAndMutationsRequireLoginAsync()
    {
        await fixture.ResetDatabaseAsync();
        (Guid systemId, Guid workItemId) = await ArrangeChangeRequestAsync("Anonymous Demo");
        using var factory = new AppSupportHubWebApplicationFactory(
            fixture.ConnectionString, interactiveLogin: false, automaticRole: null);
        using HttpClient client = factory.CreateHttpsClient(allowAutoRedirect: false);
        string[] publicRoutes =
        [
            "/", "/Systems", $"/Systems/{systemId}", "/WorkItems",
            $"/WorkItems/{workItemId}", $"/WorkItems/{workItemId}/Assessment",
            "/LegacyImports", "/api/v1/systems", "/openapi/v1.json",
            "/samples/legacy-systems.csv", "/health",
        ];
        foreach (string route in publicRoutes)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(route)).StatusCode);
        }

        using HttpResponseMessage page = await client.GetAsync("/Systems/Create");
        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
        Assert.Equal("/Account/Login", page.Headers.Location!.AbsolutePath);
        using HttpResponseMessage api = await client.PostAsJsonAsync("/api/v1/systems", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, api.StatusCode);
    }

    [Fact]
    public async Task LoginUsesGenericFailureSecureCookieLocalReturnAndPostLogoutAsync()
    {
        await fixture.ResetDatabaseAsync();
        using var factory = new AppSupportHubWebApplicationFactory(
            fixture.ConnectionString, automaticRole: null);
        using HttpClient client = factory.CreateHttpsClient(allowAutoRedirect: false);
        string html = await client.GetStringAsync("/Account/Login?returnUrl=/WorkItems");
        string token = AntiforgeryTokenExtractor.Extract(html);
        using HttpResponseMessage failed = await client.PostAsync(
            "/Account/Login",
            Form(token, AppSupportHubWebApplicationFactory.AnalystUsername, factory.AnalystPassword + "x"));
        string failure = await failed.Content.ReadAsStringAsync();
        Assert.Contains("Invalid username or password", failure, StringComparison.Ordinal);
        Assert.DoesNotContain(factory.AnalystPassword, failure, StringComparison.Ordinal);

        using HttpResponseMessage login = await LoginAsync(
            client, factory, SecurityRoles.Analyst, "/WorkItems");
        Assert.Equal("/WorkItems", login.Headers.Location!.OriginalString);
        string cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), value =>
            value.StartsWith("AppSupportHub.Session=", StringComparison.Ordinal));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        html = await client.GetStringAsync("/");
        Assert.Contains(AppSupportHubWebApplicationFactory.AnalystUsername, html, StringComparison.Ordinal);
        token = AntiforgeryTokenExtractor.Extract(html);
        using HttpResponseMessage logout = await client.PostAsync(
            "/Account/Login?handler=Logout",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
            }));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal("/Account/Login", (await client.GetAsync("/WorkItems/Create"))
            .Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task RoleMatrixProtectsSystemAndAllowsAnalystWorkAssessmentAndCsvAsync()
    {
        await fixture.ResetDatabaseAsync();
        (_, Guid workItemId) = await ArrangeChangeRequestAsync("Role Matrix");
        using var analystFactory = new AppSupportHubWebApplicationFactory(
            fixture.ConnectionString, automaticRole: SecurityRoles.Analyst);
        using HttpClient analyst = analystFactory.CreateHttpsClient(allowAutoRedirect: false);
        using HttpResponseMessage denied = await analyst.GetAsync("/Systems/Create");
        Assert.Equal("/Account/AccessDenied", denied.Headers.Location!.AbsolutePath);
        Assert.Equal(HttpStatusCode.OK, (await analyst.GetAsync("/WorkItems/Create")).StatusCode);

        string route = $"/WorkItems/{workItemId}/Assessment";
        string token = AntiforgeryTokenExtractor.Extract(await analyst.GetStringAsync(route));
        using HttpResponseMessage assessment = await analyst.PostAsync(
            route, AssessmentForm(token));
        Assert.Equal(HttpStatusCode.Redirect, assessment.StatusCode);
        token = AntiforgeryTokenExtractor.Extract(await analyst.GetStringAsync("/LegacyImports"));
        using var upload = new MultipartFormDataContent();
        upload.Add(new StringContent(token), "__RequestVerificationToken");
        upload.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(
            "LegacyId,Name,Description,Type,Criticality,LifecycleStatus,BusinessOwner,TechnicalOwner,SupportTeam,VendorName\n"
            + "L-1,Role CSV,Synthetic,Custom,Medium,Active,Business,Technical,Support,\n")),
            "Upload", "legacy.csv");
        Assert.Equal(HttpStatusCode.OK, (await analyst.PostAsync("/LegacyImports", upload)).StatusCode);

        using var adminFactory = new AppSupportHubWebApplicationFactory(fixture.ConnectionString);
        using HttpClient administrator = adminFactory.CreateHttpsClient(allowAutoRedirect: false);
        Assert.Equal(HttpStatusCode.OK, (await administrator.GetAsync("/Systems/Create")).StatusCode);
    }

    [Fact]
    public async Task UnsafeApiRequiresAntiforgeryAndHonorsRolesAsync()
    {
        await fixture.ResetDatabaseAsync();
        (Guid systemId, _) = await ArrangeChangeRequestAsync("API Security");
        using var factory = new AppSupportHubWebApplicationFactory(
            fixture.ConnectionString, automaticRole: null);
        using HttpClient client = factory.CreateHttpsClient(allowAutoRedirect: false);
        using HttpResponseMessage login = await LoginAsync(client, factory, SecurityRoles.Analyst);
        object workItem = new
        {
            applicationSystemId = systemId,
            type = "Incident",
            title = "Secured API",
            description = "Synthetic API security evidence.",
            priority = "Medium",
        };
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/v1/work-items", workItem)).StatusCode);
        await AddApiAntiforgeryAsync(client);
        Assert.Equal(
            HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/api/v1/work-items", workItem)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync("/api/v1/systems", SystemRequest("Denied System")))
            .StatusCode);

        using var adminFactory = new AppSupportHubWebApplicationFactory(fixture.ConnectionString);
        using HttpClient administrator = adminFactory.CreateHttpsClient();
        Assert.Equal(
            HttpStatusCode.Created,
            (await administrator.PostAsJsonAsync(
                "/api/v1/systems", SystemRequest("Administrator System"))).StatusCode);
    }

    [Fact]
    public async Task AuthenticatedActorPersistsForWorkItemAndAssessmentAsync()
    {
        await fixture.ResetDatabaseAsync();
        (_, Guid workItemId) = await ArrangeChangeRequestAsync("Actor Evidence");
        using var factory = new AppSupportHubWebApplicationFactory(
            fixture.ConnectionString, automaticRole: SecurityRoles.Analyst);
        using HttpClient client = factory.CreateHttpsClient(allowAutoRedirect: false);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PutAsJsonAsync(
                $"/api/v1/work-items/{workItemId}/priority",
                new { priority = "Critical", actorIdentifier = "spoofed.actor" })).StatusCode);
        string route = $"/WorkItems/{workItemId}/Assessment";
        string token = AntiforgeryTokenExtractor.Extract(await client.GetStringAsync(route));
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync(
            route, AssessmentForm(token))).StatusCode);

        await using AppSupportHubDbContext context = fixture.CreateDbContext();
        WorkItem persisted = Assert.IsType<WorkItem>(await new WorkItemRepository(context)
            .GetByIdAsync(workItemId, CancellationToken.None));
        Assert.Equal(AppSupportHubWebApplicationFactory.AnalystUsername, persisted.History.Last().ActorIdentifier);
        ChangeAssessment assessment = Assert.IsType<ChangeAssessment>(
            await new ChangeAssessmentRepository(context).GetByWorkItemIdAsync(
                workItemId, CancellationToken.None));
        Assert.Equal(AppSupportHubWebApplicationFactory.AnalystUsername, assessment.AssessedByIdentifier);
    }

    [Fact]
    public async Task HeadersRateLimitAndDisabledModeUseSecureDefaultsAsync()
    {
        await fixture.ResetDatabaseAsync();
        using var disabledFactory = new AppSupportHubWebApplicationFactory(
            fixture.ConnectionString, interactiveLogin: false, automaticRole: null);
        using HttpClient disabled = disabledFactory.CreateHttpsClient(allowAutoRedirect: false);
        using HttpResponseMessage home = await disabled.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Contains("default-src 'self'", home.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", home.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-referrer", home.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("camera=()", home.Headers.GetValues("Permissions-Policy").Single());
        Assert.False(home.Headers.Contains("Server"));
        Assert.Contains("public read-only demo", await disabled.GetStringAsync("/Account/Login"), StringComparison.OrdinalIgnoreCase);

        using var enabledFactory = new AppSupportHubWebApplicationFactory(fixture.ConnectionString, automaticRole: null);
        using HttpClient enabled = enabledFactory.CreateHttpsClient(allowAutoRedirect: false);
        string token = AntiforgeryTokenExtractor.Extract(await enabled.GetStringAsync("/Account/Login"));
        HttpResponseMessage? response = null;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            response?.Dispose();
            response = await enabled.PostAsync("/Account/Login", Form(
                token,
                AppSupportHubWebApplicationFactory.AnalystUsername,
                enabledFactory.AnalystPassword + "x"));
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, response!.StatusCode);
        Assert.DoesNotContain(enabledFactory.AnalystPassword,
            await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        response.Dispose();
    }

    private async Task<(Guid SystemId, Guid WorkItemId)> ArrangeChangeRequestAsync(string name)
    {
        ApplicationSystem system = PostgreSqlTestData.CreateApplicationSystem(name);
        WorkItem workItem = PostgreSqlTestData.CreateWorkItem(system.Id, WorkItemType.ChangeRequest);
        await using AppSupportHubDbContext context = fixture.CreateDbContext();
        await new ApplicationSystemRepository(context).AddAsync(system, CancellationToken.None);
        await new WorkItemRepository(context).AddAsync(workItem, CancellationToken.None);
        await context.SaveChangesAsync();
        return (system.Id, workItem.Id);
    }

    private static async Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        AppSupportHubWebApplicationFactory factory,
        string role,
        string? returnUrl = null)
    {
        string token = AntiforgeryTokenExtractor.Extract(await client.GetStringAsync(
            "/Account/Login" + (returnUrl is null ? string.Empty : $"?returnUrl={returnUrl}")));
        return await client.PostAsync("/Account/Login", Form(
            token,
            role == SecurityRoles.Analyst
                ? AppSupportHubWebApplicationFactory.AnalystUsername
                : AppSupportHubWebApplicationFactory.AdministratorUsername,
            role == SecurityRoles.Analyst ? factory.AnalystPassword : factory.AdministratorPassword,
            returnUrl));
    }

    private static async Task AddApiAntiforgeryAsync(HttpClient client)
    {
        JsonElement token = await client.GetFromJsonAsync<JsonElement>("/api/v1/security/antiforgery");
        client.DefaultRequestHeaders.Add(token.GetProperty("headerName").GetString()!, token.GetProperty("requestToken").GetString());
    }

    private static FormUrlEncodedContent Form(
        string token, string username, string password, string? returnUrl = null) =>
        new(new Dictionary<string, string>
        {
            ["Input.Username"] = username,
            ["Input.Password"] = password,
            ["ReturnUrl"] = returnUrl ?? string.Empty,
            ["__RequestVerificationToken"] = token,
        });

    private static FormUrlEncodedContent AssessmentForm(string token) => new(
        new Dictionary<string, string>
        {
            ["Input.BusinessNeed"] = "Synthetic business need",
            ["Input.TechnicalImpact"] = "Synthetic technical impact",
            ["Input.SecurityImpact"] = "Synthetic security impact",
            ["Input.Risk"] = "High",
            ["Input.AcceptanceCriteria"] = "Synthetic acceptance criteria",
            ["Input.TestPlan"] = "Synthetic test plan",
            ["Input.RollbackPlan"] = "Synthetic rollback plan",
            ["__RequestVerificationToken"] = token,
        });

    private static object SystemRequest(string name) => new
    {
        name,
        description = "Synthetic secured system.",
        type = "Custom",
        criticality = "Medium",
        initialLifecycleStatus = "Active",
        businessOwner = "Business",
        technicalOwner = "Technical",
        supportTeam = "Support",
    };
}
