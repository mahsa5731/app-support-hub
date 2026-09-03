using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using AppSupportHub.Application.Systems.Queries;
using AppSupportHub.Application.WorkItems.Queries;
using AppSupportHub.Application.WorkItems.ReadModels;
using AppSupportHub.Infrastructure.Persistence;
using AppSupportHub.IntegrationTests.Persistence;
using AppSupportHub.Web.DemoData;
using AppSupportHub.Web.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppSupportHub.IntegrationTests.Web;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class WebHttpTests(PostgreSqlContainerFixture database) : IAsyncLifetime
{
    private const string AuthenticatedActorIdentifier =
        AppSupportHubWebApplicationFactory.AdministratorUsername;

    public Task InitializeAsync() => database.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public void CompositionResolvesPagesEndpointsAndSharedScopedPersistence()
    {
        using var factory = new AppSupportHubWebApplicationFactory(database.ConnectionString);
        using IServiceScope scope = factory.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        Type[] pageModels =
        [
            typeof(AppSupportHub.Web.Pages.Systems.IndexModel),
            typeof(AppSupportHub.Web.Pages.Systems.CreateModel),
            typeof(AppSupportHub.Web.Pages.Systems.DetailsModel),
            typeof(AppSupportHub.Web.Pages.Systems.EditModel),
            typeof(AppSupportHub.Web.Pages.WorkItems.IndexModel),
            typeof(AppSupportHub.Web.Pages.WorkItems.CreateModel),
            typeof(AppSupportHub.Web.Pages.WorkItems.DetailsModel),
            typeof(AppSupportHub.Web.Pages.WorkItems.EditModel),
        ];

        Assert.All(pageModels, type => Assert.NotNull(
            ActivatorUtilities.CreateInstance(services, type)));

        string[] apiRoutes = factory.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .OfType<string>()
            .Where(route => route.StartsWith("/api/v1/", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(15, apiRoutes.Length);

        AppSupportHubDbContext dbContext = services.GetRequiredService<AppSupportHubDbContext>();
        IApplicationSystemQueries systemQueries =
            services.GetRequiredService<IApplicationSystemQueries>();
        IWorkItemQueries workItemQueries = services.GetRequiredService<IWorkItemQueries>();
        Assert.Same(dbContext, GetDbContext(systemQueries));
        Assert.Same(dbContext, GetDbContext(workItemQueries));
    }

    [Fact]
    public async Task PrimaryPagesAndOpenApiReturnSuccessOverHttpsAsync()
    {
        using var factory = new AppSupportHubWebApplicationFactory(database.ConnectionString);
        using HttpClient client = factory.CreateHttpsClient();
        Guid systemId = await CreateSystemAsync(client, "Route Evidence System");
        Guid workItemId = await CreateWorkItemAsync(client, systemId, "Route evidence incident");

        string[] routes =
        [
            "/",
            "/Systems",
            $"/Systems/{systemId}",
            "/WorkItems",
            $"/WorkItems/{workItemId}",
            "/openapi/v1.json",
        ];

        foreach (string route in routes)
        {
            using HttpResponseMessage response = await client.GetAsync(route);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("https", response.RequestMessage!.RequestUri!.Scheme);
        }
    }

    [Fact]
    public async Task SystemsRazorJourneyUsesAntiforgeryPrgAndPersistsAsync()
    {
        using var factory = new AppSupportHubWebApplicationFactory(database.ConnectionString);
        using HttpClient client = factory.CreateHttpsClient(allowAutoRedirect: false);

        string createHtml = await client.GetStringAsync("/Systems/Create");
        string createToken = AntiforgeryTokenExtractor.Extract(createHtml);
        using HttpResponseMessage created = await PostFormAsync(
            client,
            "/Systems/Create",
            createToken,
            new Dictionary<string, string>
            {
                ["Input.Name"] = "Razor Journey System",
                ["Input.Description"] = "Synthetic system created through a real Razor form.",
                ["Input.Type"] = "Custom",
                ["Input.Criticality"] = "High",
                ["InitialLifecycleStatus"] = "Active",
                ["Input.BusinessOwner"] = "Synthetic Business",
                ["Input.TechnicalOwner"] = "Synthetic Technical",
                ["Input.SupportTeam"] = "Synthetic Support",
                ["Input.VendorName"] = string.Empty,
            });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        Guid id = IdFromLocation(created.Headers.Location);

        string editHtml = await client.GetStringAsync($"/Systems/{id}/Edit");
        string editToken = AntiforgeryTokenExtractor.Extract(editHtml);
        using HttpResponseMessage edited = await PostFormAsync(
            client,
            $"/Systems/{id}/Edit",
            editToken,
            new Dictionary<string, string>
            {
                ["Input.Name"] = "Razor Journey System Updated",
                ["Input.Description"] = "Updated through the server-rendered form.",
                ["Input.Type"] = "Custom",
                ["Input.Criticality"] = "Critical",
                ["Input.BusinessOwner"] = "Synthetic Business",
                ["Input.TechnicalOwner"] = "Synthetic Technical",
                ["Input.SupportTeam"] = "Synthetic Support",
                ["Input.VendorName"] = string.Empty,
            });
        Assert.Equal(HttpStatusCode.Redirect, edited.StatusCode);
        Assert.Equal($"/Systems/{id}", edited.Headers.Location!.OriginalString);

        string detailHtml = await client.GetStringAsync($"/Systems/{id}");
        string lifecycleToken = AntiforgeryTokenExtractor.Extract(detailHtml);
        using HttpResponseMessage transitioned = await PostFormAsync(
            client,
            $"/Systems/{id}?handler=Lifecycle",
            lifecycleToken,
            new Dictionary<string, string>
            {
                ["TargetLifecycleStatus"] = "Maintenance",
                ["RetirementReason"] = string.Empty,
                ["ConfirmLifecycleChange"] = "true",
            });
        Assert.Equal(HttpStatusCode.Redirect, transitioned.StatusCode);

        JsonElement system = await GetJsonAsync(client, $"/api/v1/systems/{id}");
        Assert.Equal("Razor Journey System Updated", system.GetProperty("name").GetString());
        Assert.Equal("Critical", system.GetProperty("criticality").GetString());
        Assert.Equal("Maintenance", system.GetProperty("lifecycleStatus").GetString());
    }

    [Fact]
    public async Task WorkItemRazorJourneyUsesAntiforgeryPrgAndPersistsHistoryAsync()
    {
        using var factory = new AppSupportHubWebApplicationFactory(database.ConnectionString);
        using HttpClient client = factory.CreateHttpsClient(allowAutoRedirect: false);
        Guid systemId = await CreateSystemAsync(client, "Razor Work System");

        string createHtml = await client.GetStringAsync($"/WorkItems/Create?applicationSystemId={systemId}");
        string token = AntiforgeryTokenExtractor.Extract(createHtml);
        using HttpResponseMessage created = await PostFormAsync(
            client,
            "/WorkItems/Create",
            token,
            new Dictionary<string, string>
            {
                ["Input.ApplicationSystemId"] = systemId.ToString(),
                ["Input.Type"] = "Incident",
                ["Input.Title"] = "Razor synthetic incident",
                ["Input.Description"] = "Created through a real antiforgery-protected form.",
                ["Input.Priority"] = "High",
                ["Input.DueAtUtc"] = "2035-02-03T12:30",
            });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        Guid id = IdFromLocation(created.Headers.Location);

        string detailHtml = await client.GetStringAsync($"/WorkItems/{id}");
        token = AntiforgeryTokenExtractor.Extract(detailHtml);
        using HttpResponseMessage assigned = await PostFormAsync(
            client,
            $"/WorkItems/{id}?handler=Assign",
            token,
            new Dictionary<string, string>
            {
                ["AssigneeIdentifier"] = "synthetic.specialist",
            });
        Assert.Equal(HttpStatusCode.Redirect, assigned.StatusCode);

        using HttpResponseMessage triaged = await PostFormAsync(
            client,
            $"/WorkItems/{id}?handler=Transition",
            token,
            new Dictionary<string, string>
            {
                ["TargetStatus"] = "UnderAnalysis",
                ["Comment"] = "Synthetic triage",
                ["ResolutionSummary"] = string.Empty,
            });
        Assert.Equal(HttpStatusCode.Redirect, triaged.StatusCode);

        string persistedHtml = await client.GetStringAsync($"/WorkItems/{id}");
        Assert.Contains("synthetic.specialist", persistedHtml, StringComparison.Ordinal);
        Assert.Contains("UnderAnalysis", persistedHtml, StringComparison.Ordinal);
        Assert.Contains("Immutable history", persistedHtml, StringComparison.Ordinal);
        Assert.Contains(AuthenticatedActorIdentifier, persistedHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidRazorFormShowsLinkedFeedbackWithoutMutationAsync()
    {
        using var factory = new AppSupportHubWebApplicationFactory(database.ConnectionString);
        using HttpClient client = factory.CreateHttpsClient(allowAutoRedirect: false);
        string html = await client.GetStringAsync("/Systems/Create");
        string token = AntiforgeryTokenExtractor.Extract(html);

        using HttpResponseMessage response = await PostFormAsync(
            client,
            "/Systems/Create",
            token,
            new Dictionary<string, string>
            {
                ["Input.Name"] = string.Empty,
                ["Input.Type"] = "not-a-type",
                ["InitialLifecycleStatus"] = "Active",
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string invalidHtml = await response.Content.ReadAsStringAsync();
        Assert.Contains("field is required", invalidHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-valmsg-for=\"Input.Name\"", invalidHtml, StringComparison.Ordinal);
        JsonElement systems = await GetJsonAsync(client, "/api/v1/systems");
        Assert.Equal(0, systems.GetArrayLength());
    }

    [Fact]
    public async Task EveryApiRouteIsReachableAndRepresentativeMutationsPersistAsync()
    {
        using var factory = new AppSupportHubWebApplicationFactory(database.ConnectionString);
        using HttpClient client = factory.CreateHttpsClient();

        using HttpResponseMessage systemCreated = await client.PostAsJsonAsync(
            "/api/v1/systems",
            new
            {
                name = "Complete API Route System",
                description = "Synthetic system used to reach every v1 route.",
                type = "Custom",
                criticality = "High",
                initialLifecycleStatus = "Active",
                businessOwner = "Synthetic Business",
                technicalOwner = "Synthetic Technical",
                supportTeam = "Synthetic Support",
                vendorName = (string?)null,
            });
        Assert.Equal(HttpStatusCode.Created, systemCreated.StatusCode);
        JsonElement createdSystemBody =
            await systemCreated.Content.ReadFromJsonAsync<JsonElement>();
        Guid systemId = createdSystemBody.GetProperty("id").GetGuid();
        Assert.Equal(
            $"/api/v1/systems/{systemId}",
            systemCreated.Headers.Location!.OriginalString);

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.GetAsync("/api/v1/systems?limit=1")).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.GetAsync($"/api/v1/systems/{systemId}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PutAsJsonAsync(
                $"/api/v1/systems/{systemId}",
                new
                {
                    name = "Complete API Route System Updated",
                    description = "Updated through the versioned API.",
                    type = "Custom",
                    criticality = "Critical",
                    businessOwner = "Synthetic Business",
                    technicalOwner = "Synthetic Technical",
                    supportTeam = "Synthetic Support",
                    vendorName = (string?)null,
                })).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsJsonAsync(
                $"/api/v1/systems/{systemId}/lifecycle",
                new { targetLifecycleStatus = "Maintenance", retirementReason = (string?)null }))
            .StatusCode);

        using HttpResponseMessage workItemCreated = await client.PostAsJsonAsync(
            "/api/v1/work-items",
            new
            {
                applicationSystemId = systemId,
                type = "ChangeRequest",
                title = "Reach all work-item API routes",
                description = "Synthetic work item used by a route-coverage test.",
                priority = "High",
                dueAt = "2035-03-04T15:00:00+00:00",
            });
        Assert.Equal(HttpStatusCode.Created, workItemCreated.StatusCode);
        JsonElement createdWorkItemBody =
            await workItemCreated.Content.ReadFromJsonAsync<JsonElement>();
        Guid workItemId = createdWorkItemBody.GetProperty("id").GetGuid();
        Assert.Equal(
            $"/api/v1/work-items/{workItemId}",
            workItemCreated.Headers.Location!.OriginalString);

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.GetAsync("/api/v1/work-items?type=ChangeRequest&limit=1"))
            .StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.GetAsync($"/api/v1/work-items/{workItemId}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PutAsJsonAsync(
                $"/api/v1/work-items/{workItemId}",
                new
                {
                    title = "Every work-item API route reached",
                    description = "Updated through the representative API path.",
                })).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PutAsJsonAsync(
                $"/api/v1/work-items/{workItemId}/assignment",
                new { assigneeIdentifier = "synthetic.api.specialist" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.DeleteAsync($"/api/v1/work-items/{workItemId}/assignment"))
            .StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PutAsJsonAsync(
                $"/api/v1/work-items/{workItemId}/priority",
                new { priority = "Critical" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PutAsJsonAsync(
                $"/api/v1/work-items/{workItemId}/due-date",
                new { dueAt = "2035-04-05T16:30:00-05:00" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsJsonAsync(
                $"/api/v1/work-items/{workItemId}/transitions",
                new
                {
                    targetStatus = "UnderAnalysis",
                    comment = "Synthetic API triage",
                    resolutionSummary = (string?)null,
                })).StatusCode);

        JsonElement persisted = await GetJsonAsync(
            client,
            $"/api/v1/work-items/{workItemId}");
        Assert.Equal("Every work-item API route reached", persisted.GetProperty("title").GetString());
        Assert.Equal("Critical", persisted.GetProperty("priority").GetString());
        Assert.Equal("UnderAnalysis", persisted.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ApiErrorsUseExpectedProblemStatusesAndStableCodesAsync()
    {
        using var factory = new AppSupportHubWebApplicationFactory(database.ConnectionString);
        using HttpClient client = factory.CreateHttpsClient();

        using HttpResponseMessage invalid = await client.PostAsJsonAsync(
            "/api/v1/systems",
            new
            {
                name = "Invalid Vocabulary",
                description = "Synthetic invalid request.",
                type = "NoSuchType",
                criticality = "High",
                initialLifecycleStatus = "Active",
                businessOwner = "Business",
                technicalOwner = "Technical",
                supportTeam = "Support",
            });
        await AssertProblemAsync(
            invalid,
            HttpStatusCode.BadRequest,
            "validation.invalid_input");

        using HttpResponseMessage missing = await client.GetAsync(
            $"/api/v1/work-items/{Guid.NewGuid()}");
        await AssertProblemAsync(missing, HttpStatusCode.NotFound, "work_items.not_found");

        await CreateSystemAsync(client, "API Conflict System");
        using HttpResponseMessage conflict = await client.PostAsJsonAsync(
            "/api/v1/systems",
            new
            {
                name = "API Conflict System",
                description = "Duplicate synthetic request.",
                type = "Custom",
                criticality = "High",
                initialLifecycleStatus = "Active",
                businessOwner = "Business",
                technicalOwner = "Technical",
                supportTeam = "Support",
            });
        await AssertProblemAsync(conflict, HttpStatusCode.Conflict, "systems.name_conflict");

        Guid systemId = await CreateSystemAsync(client, "API Business Rule System");
        Guid workItemId = await CreateWorkItemAsync(client, systemId, "Invalid transition evidence");
        using HttpResponseMessage businessRule = await client.PostAsJsonAsync(
            $"/api/v1/work-items/{workItemId}/transitions",
            new
            {
                targetStatus = "Resolved",
                comment = "Cannot resolve directly from New.",
                resolutionSummary = "Synthetic resolution",
            });
        await AssertProblemAsync(
            businessRule,
            HttpStatusCode.Conflict,
            "work_items.invalid_transition");
    }

    [Fact]
    public async Task ApiIgnoresActorSpoofingAndPersistsOnlyAuthenticatedActorAsync()
    {
        using var factory = new AppSupportHubWebApplicationFactory(database.ConnectionString);
        using HttpClient client = factory.CreateHttpsClient();
        Guid systemId = await CreateSystemAsync(client, "Actor Boundary System");

        using HttpResponseMessage created = await client.PostAsJsonAsync(
            "/api/v1/work-items",
            new
            {
                applicationSystemId = systemId,
                type = "Incident",
                title = "Actor boundary incident",
                description = "Synthetic actor-spoofing boundary evidence.",
                priority = "High",
                dueAt = (string?)null,
                actorIdentifier = "spoofed.external.actor",
            });
        created.EnsureSuccessStatusCode();
        Guid id = (await created.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetGuid();

        using HttpResponseMessage assigned = await client.PutAsJsonAsync(
            $"/api/v1/work-items/{id}/assignment",
            new
            {
                assigneeIdentifier = "synthetic.assignee",
                actorIdentifier = "another.spoofed.actor",
            });
        assigned.EnsureSuccessStatusCode();

        JsonElement detail = await GetJsonAsync(client, $"/api/v1/work-items/{id}");
        string[] actors = detail.GetProperty("history")
            .EnumerateArray()
            .Select(item => item.GetProperty("actorIdentifier").GetString()!)
            .ToArray();
        Assert.NotEmpty(actors);
        Assert.All(actors, actor => Assert.Equal(AuthenticatedActorIdentifier, actor));
        Assert.DoesNotContain("spoofed.external.actor", detail.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HttpListLimitsRemainBoundedAndCancellationReachesQueryAsync()
    {
        using var factory = new AppSupportHubWebApplicationFactory(database.ConnectionString);
        using HttpClient client = factory.CreateHttpsClient();
        Guid systemId = await CreateSystemAsync(client, "Bounded List System");
        await CreateWorkItemAsync(client, systemId, "Bounded list item one");
        await CreateWorkItemAsync(client, systemId, "Bounded list item two");

        JsonElement limited = await GetJsonAsync(client, "/api/v1/work-items?limit=1");
        Assert.Equal(1, limited.GetArrayLength());
        using HttpResponseMessage invalidLimit = await client.GetAsync(
            "/api/v1/work-items?limit=101");
        await AssertProblemAsync(
            invalidLimit,
            HttpStatusCode.BadRequest,
            "validation.invalid_input");

        var observer = new CancellationObservingWorkItemQueries();
        using var cancellationFactory = new AppSupportHubWebApplicationFactory(
            database.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<IWorkItemQueries>();
                services.AddScoped<IWorkItemQueries>(_ => observer);
            });
        using HttpClient cancellationClient = cancellationFactory.CreateHttpsClient();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        Task<HttpResponseMessage> request = cancellationClient.GetAsync(
            "/api/v1/work-items",
            cancellation.Token);
        Assert.True(await observer.CancellationTokenObserved.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
    }

    [Fact]
    public async Task HistoryOrderIsStableWhenTimestampsMatchAsync()
    {
        var fixedTimeProvider = new FixedTimeProvider(
            new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));
        using var factory = new AppSupportHubWebApplicationFactory(
            database.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(fixedTimeProvider);
            });
        using HttpClient client = factory.CreateHttpsClient();
        Guid systemId = await CreateSystemAsync(client, "Stable History System");
        Guid id = await CreateWorkItemAsync(client, systemId, "Stable history incident");

        (await client.PutAsJsonAsync(
            $"/api/v1/work-items/{id}/assignment",
            new { assigneeIdentifier = "synthetic.history.owner" })).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync(
            $"/api/v1/work-items/{id}/priority",
            new { priority = "Critical" })).EnsureSuccessStatusCode();

        JsonElement detail = await GetJsonAsync(client, $"/api/v1/work-items/{id}");
        JsonElement.ArrayEnumerator history = detail.GetProperty("history").EnumerateArray();
        string[] eventTypes = history
            .Select(item => item.GetProperty("eventType").GetString()!)
            .ToArray();
        DateTimeOffset[] timestamps = detail.GetProperty("history")
            .EnumerateArray()
            .Select(item => item.GetProperty("occurredAtUtc").GetDateTimeOffset())
            .ToArray();

        Assert.Equal(["Created", "Assigned", "PriorityChanged"], eventTypes);
        Assert.Single(timestamps.Distinct());
    }

    [Theory]
    [InlineData("Testing", false)]
    [InlineData("Production", true)]
    public async Task DemoSeedDoesNotRunUnlessDevelopmentAndEnabledAsync(
        string environment,
        bool enabled)
    {
        using var factory = new AppSupportHubWebApplicationFactory(
            database.ConnectionString,
            environment,
            enabled);
        using HttpClient client = factory.CreateHttpsClient();

        JsonElement systems = await GetJsonAsync(client, "/api/v1/systems");
        JsonElement workItems = await GetJsonAsync(client, "/api/v1/work-items");
        Assert.Equal(0, systems.GetArrayLength());
        Assert.Equal(0, workItems.GetArrayLength());
    }

    [Fact]
    public async Task EnabledDevelopmentSeedIsSyntheticCompleteAndIdempotentAsync()
    {
        using var factory = new AppSupportHubWebApplicationFactory(
            database.ConnectionString,
            "Development",
            seedDemoData: true);
        using HttpClient client = factory.CreateHttpsClient();

        JsonElement systems = await GetJsonAsync(client, "/api/v1/systems?limit=100");
        JsonElement workItems = await GetJsonAsync(client, "/api/v1/work-items?limit=100");
        Assert.Equal(3, systems.GetArrayLength());
        Assert.Equal(5, workItems.GetArrayLength());
        Assert.Contains(
            systems.EnumerateArray(),
            item => item.GetProperty("type").GetString() == "Commercial");
        Assert.Contains(
            workItems.EnumerateArray(),
            item => item.GetProperty("type").GetString() == "Incident");
        Assert.Contains(
            workItems.EnumerateArray(),
            item => item.GetProperty("type").GetString() == "Enhancement");
        Assert.Contains(
            workItems.EnumerateArray(),
            item => item.GetProperty("type").GetString() == "ChangeRequest");

        JsonElement meaningfulItem = workItems.EnumerateArray().Single(item =>
            item.GetProperty("title").GetString() == "Investigate synthetic export delay");
        Guid meaningfulId = meaningfulItem.GetProperty("id").GetGuid();
        int historyCount = (await GetJsonAsync(client, $"/api/v1/work-items/{meaningfulId}"))
            .GetProperty("history")
            .GetArrayLength();

        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>()
                .SeedAsync(CancellationToken.None);
        }

        Assert.Equal(
            3,
            (await GetJsonAsync(client, "/api/v1/systems?limit=100")).GetArrayLength());
        Assert.Equal(
            5,
            (await GetJsonAsync(client, "/api/v1/work-items?limit=100")).GetArrayLength());
        Assert.Equal(
            historyCount,
            (await GetJsonAsync(client, $"/api/v1/work-items/{meaningfulId}"))
            .GetProperty("history")
            .GetArrayLength());
    }

    [Fact]
    public async Task RenderedPagesExposeBasicAccessibilityAndHonestScopeEvidenceAsync()
    {
        using var factory = new AppSupportHubWebApplicationFactory(database.ConnectionString);
        using HttpClient client = factory.CreateHttpsClient();

        string home = await client.GetStringAsync("/");
        string create = await client.GetStringAsync("/Systems/Create");
        string css = await client.GetStringAsync("/css/site.css");

        Assert.Contains("<html lang=\"en\">", home, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(home, "<h1"));
        Assert.Contains("<nav", home, StringComparison.Ordinal);
        Assert.Contains("<main", home, StringComparison.Ordinal);
        Assert.Contains("<footer", home, StringComparison.Ordinal);
        Assert.Contains("Skip to main content", home, StringComparison.Ordinal);
        Assert.Contains("Live portfolio demo", home, StringComparison.Ordinal);
        Assert.Contains("Public read-only portfolio demo", home, StringComparison.Ordinal);
        Assert.Contains("not affiliated", home, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, CountOccurrences(create, "<h1"));
        Assert.Contains("<label", create, StringComparison.Ordinal);
        Assert.Contains("data-valmsg-for=\"Input.Name\"", create, StringComparison.Ordinal);
        Assert.Contains(":focus-visible", css, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HealthIsLivenessOnlyAndDoesNotRequirePostgreSqlAsync()
    {
        const string unreachableConnection =
            "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1";
        using var factory = new AppSupportHubWebApplicationFactory(unreachableConnection);
        using HttpClient client = factory.CreateHttpsClient();

        using HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));
    }

    private static int CountOccurrences(string value, string search)
    {
        return value.Split(search, StringSplitOptions.None).Length - 1;
    }

    private static AppSupportHubDbContext GetDbContext(object service)
    {
        FieldInfo field = service.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.FieldType == typeof(AppSupportHubDbContext));
        return Assert.IsType<AppSupportHubDbContext>(field.GetValue(service));
    }

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client,
        string route,
        string token,
        IReadOnlyDictionary<string, string> values)
    {
        IEnumerable<KeyValuePair<string, string>> form = values
            .Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value))
            .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token));
        return await client.PostAsync(route, new FormUrlEncodedContent(form));
    }

    private static Guid IdFromLocation(Uri? location)
    {
        Assert.NotNull(location);
        return Guid.Parse(location.OriginalString.TrimEnd('/').Split('/').Last());
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string route)
    {
        using HttpResponseMessage response = await client.GetAsync(route);
        Assert.True(
            response.IsSuccessStatusCode,
            $"GET {route} returned {(int)response.StatusCode}: "
            + await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<Guid> CreateSystemAsync(HttpClient client, string name)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/systems",
            new
            {
                name,
                description = "Synthetic system created by an HTTP integration test.",
                type = "Custom",
                criticality = "High",
                initialLifecycleStatus = "Active",
                businessOwner = "Synthetic Business",
                technicalOwner = "Synthetic Technical",
                supportTeam = "Synthetic Support",
                vendorName = (string?)null,
            });
        response.EnsureSuccessStatusCode();
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateWorkItemAsync(
        HttpClient client,
        Guid systemId,
        string title)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/work-items",
            new
            {
                applicationSystemId = systemId,
                type = "Incident",
                title,
                description = "Synthetic work item created by an HTTP integration test.",
                priority = "High",
                dueAt = "2035-02-03T12:30:00Z",
            });
        response.EnsureSuccessStatusCode();
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class CancellationObservingWorkItemQueries : IWorkItemQueries
    {
        internal TaskCompletionSource<bool> CancellationTokenObserved { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<WorkItemDetail?> GetByIdAsync(
            Guid id,
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<WorkItemDetail?>(null);
        }

        public async Task<IReadOnlyList<WorkItemSummary>> ListAsync(
            WorkItemQueryFilter filter,
            CancellationToken cancellationToken)
        {
            CancellationTokenObserved.TrySetResult(cancellationToken.CanBeCanceled);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return [];
        }
    }
}
