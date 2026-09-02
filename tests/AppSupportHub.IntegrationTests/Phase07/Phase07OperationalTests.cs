using System.Net;
using AppSupportHub.Application.Operations;
using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.Infrastructure.Persistence;
using AppSupportHub.Infrastructure.Persistence.Queries.Operations;
using AppSupportHub.IntegrationTests.Persistence;
using AppSupportHub.IntegrationTests.Web;
using AppSupportHub.Web.Operations;

namespace AppSupportHub.IntegrationTests.Phase07;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class Phase07OperationalTests(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public async Task OverviewUsesServerCountsBoundedOrderOverdueDaysAndCancellationAsync()
    {
        await fixture.ResetDatabaseAsync();
        DateTimeOffset asOfUtc = new(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);
        ApplicationSystem active = PostgreSqlTestData.CreateApplicationSystem("Operations Active");
        ApplicationSystem planned = PostgreSqlTestData.CreateApplicationSystem(
            "Operations Planned",
            lifecycleStatus: ApplicationLifecycleStatus.Planned);
        WorkItem[] openItems = Enumerable.Range(1, 7)
            .Select(index => PostgreSqlTestData.CreateWorkItem(
                active.Id,
                index == 1 ? WorkItemType.ChangeRequest : WorkItemType.Incident,
                asOfUtc.AddDays(-index)))
            .ToArray();
        Assert.True(openItems[0].ChangePriority(
            WorkItemPriority.Critical,
            "operations.test",
            asOfUtc.AddHours(-1)));
        WorkItem cancelled = PostgreSqlTestData.CreateWorkItem(
            planned.Id,
            dueAtUtc: asOfUtc.AddDays(-10));
        Assert.True(cancelled.TransitionTo(
            WorkItemStatus.Cancelled,
            "operations.test",
            asOfUtc.AddHours(-1),
            "Synthetic cancellation."));

        await using (AppSupportHubDbContext context = fixture.CreateDbContext())
        {
            context.AddRange(active, planned);
            await context.SaveChangesAsync();
            context.AddRange(openItems.Cast<object>().Append(cancelled));
            await context.SaveChangesAsync();
        }

        await using AppSupportHubDbContext queryContext = fixture.CreateDbContext();
        var handler = new GetOperationsOverviewHandler(
            new OperationsOverviewQueries(queryContext),
            new FixedTimeProvider(asOfUtc));
        OperationsOverview overview = await handler.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, overview.TotalApplicationSystems);
        Assert.Equal(1, overview.ActiveApplicationSystems);
        Assert.Equal(7, overview.OpenWorkItems);
        Assert.Equal(1, overview.CriticalOpenWorkItems);
        Assert.Equal(7, overview.OverdueOpenWorkItems);
        Assert.Equal(1, overview.ChangeRequestWorkItems);
        Assert.Equal(5, overview.MostOverdueOpenWorkItems.Count);
        Guid[] expectedIds = openItems.OrderBy(item => item.DueAtUtc).ThenBy(item => item.Id)
            .Take(5).Select(item => item.Id).ToArray();
        Assert.Equal(expectedIds, overview.MostOverdueOpenWorkItems.Select(item => item.Id));
        Assert.Equal(7, overview.MostOverdueOpenWorkItems[0].OverdueDays);
        Assert.All(overview.MostOverdueOpenWorkItems,
            item => Assert.Equal(active.Name, item.ApplicationSystemName));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.ExecuteAsync(cancellation.Token));
    }

    [Fact]
    public async Task OperationsPageIsPublicBoundedFictionalAndEmptyStateSafeAsync()
    {
        await fixture.ResetDatabaseAsync();
        using var factory = new AppSupportHubWebApplicationFactory(
            fixture.ConnectionString,
            "Development",
            seedDemoData: true,
            automaticRole: null);
        using HttpClient client = factory.CreateHttpsClient();

        using HttpResponseMessage response = await client.GetAsync("/Operations");
        string html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-summary=\"total-systems\">3", html, StringComparison.Ordinal);
        Assert.Contains("data-summary=\"active-systems\">2", html, StringComparison.Ordinal);
        Assert.Contains("data-summary=\"open-work-items\">5", html, StringComparison.Ordinal);
        Assert.Contains("data-summary=\"critical-open\">1", html, StringComparison.Ordinal);
        Assert.Contains("data-summary=\"overdue-open\">0", html, StringComparison.Ordinal);
        Assert.Contains("data-summary=\"change-requests\">1", html, StringComparison.Ordinal);
        Assert.Contains("No overdue open work items", html, StringComparison.Ordinal);
        Assert.Contains("UTC", html, StringComparison.Ordinal);
        Assert.Contains("aria-current=\"page\"", html, StringComparison.Ordinal);
        Assert.Contains("fictional portfolio data", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not affiliated", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionStrings", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HealthSeparatesDatabaseIndependentLivenessFromSafeReadinessAsync()
    {
        await fixture.ResetDatabaseAsync();
        using var reachableFactory = new AppSupportHubWebApplicationFactory(
            fixture.ConnectionString,
            interactiveLogin: false,
            automaticRole: null);
        using HttpClient reachable = reachableFactory.CreateHttpsClient();
        Assert.Equal("Healthy", await reachable.GetStringAsync("/health"));
        Assert.Equal("Healthy", await reachable.GetStringAsync("/health/ready"));

        const string unreachableConnection =
            "Host=127.0.0.1;Port=1;Database=unreachable;Username=none;Password=none;Timeout=1";
        using var unreachableFactory = new AppSupportHubWebApplicationFactory(
            unreachableConnection,
            interactiveLogin: false,
            automaticRole: null);
        using HttpClient unreachable = unreachableFactory.CreateHttpsClient();
        using HttpResponseMessage liveness = await unreachable.GetAsync("/health");
        using HttpResponseMessage readiness = await unreachable.GetAsync("/health/ready");
        string readinessBody = await readiness.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);
        Assert.Equal("Healthy", await liveness.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
        Assert.Equal("Unhealthy", readinessBody);
        Assert.DoesNotContain("Host", readinessBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", readinessBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", readinessBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CorrelationIdsAreGeneratedAcceptedNormalizedAndSafelyReplacedAsync()
    {
        using var factory = new AppSupportHubWebApplicationFactory(
            fixture.ConnectionString,
            interactiveLogin: false,
            automaticRole: null);
        using HttpClient client = factory.CreateHttpsClient();

        using HttpResponseMessage generated = await client.GetAsync("/health");
        string generatedId = CorrelationId(generated);
        Assert.True(Guid.TryParseExact(generatedId, "N", out _));
        Assert.Equal(generatedId.ToLowerInvariant(), generatedId);

        var supplied = Guid.NewGuid();
        using var acceptedRequest = new HttpRequestMessage(HttpMethod.Get, "/");
        acceptedRequest.Headers.Add(RequestCorrelationMiddleware.HeaderName, supplied.ToString("D").ToUpperInvariant());
        using HttpResponseMessage accepted = await client.SendAsync(acceptedRequest);
        Assert.Equal(supplied.ToString("N"), CorrelationId(accepted));

        using var invalidRequest = new HttpRequestMessage(HttpMethod.Get, "/health");
        invalidRequest.Headers.Add(RequestCorrelationMiddleware.HeaderName, "not-a-correlation-id");
        using HttpResponseMessage replaced = await client.SendAsync(invalidRequest);
        string replacement = CorrelationId(replaced);
        Assert.True(Guid.TryParseExact(replacement, "N", out _));
        Assert.NotEqual("not-a-correlation-id", replacement);
        Assert.Contains("default-src 'self'", accepted.Headers
            .GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
    }

    private static string CorrelationId(HttpResponseMessage response) =>
        response.Headers.GetValues(RequestCorrelationMiddleware.HeaderName).Single();

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
