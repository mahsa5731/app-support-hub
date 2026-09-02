using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;

namespace AppSupportHub.IntegrationTests.Persistence;

internal static class PostgreSqlTestData
{
    internal static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 15, 14, 30, 0, TimeSpan.Zero);

    internal static ApplicationSystem CreateApplicationSystem(
        string name = "Synthetic Support Portal",
        ApplicationSystemType type = ApplicationSystemType.Custom,
        ApplicationLifecycleStatus lifecycleStatus = ApplicationLifecycleStatus.Active,
        string? vendorName = null)
    {
        return ApplicationSystem.Create(
            name,
            "Synthetic application used only by PostgreSQL integration tests.",
            type,
            ApplicationCriticality.High,
            lifecycleStatus,
            "Synthetic Business Owner",
            "Synthetic Technical Owner",
            "Synthetic Support Team",
            vendorName,
            CreatedAtUtc);
    }

    internal static WorkItem CreateWorkItem(
        Guid applicationSystemId,
        WorkItemType type = WorkItemType.Incident,
        DateTimeOffset? dueAtUtc = null)
    {
        return WorkItem.Create(
            applicationSystemId,
            type,
            "Synthetic incident",
            "Synthetic work item used only by PostgreSQL integration tests.",
            WorkItemPriority.High,
            dueAtUtc ?? CreatedAtUtc.AddDays(2),
            "synthetic.creator",
            CreatedAtUtc);
    }
}
