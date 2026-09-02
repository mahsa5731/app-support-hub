using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.CreateApplicationSystem;
using AppSupportHub.Application.Systems.Inputs;
using AppSupportHub.Application.Systems.ListApplicationSystems;
using AppSupportHub.Application.Systems.ReadModels;
using AppSupportHub.Application.WorkItems.AssignWorkItem;
using AppSupportHub.Application.WorkItems.ChangeWorkItemDueDate;
using AppSupportHub.Application.WorkItems.ChangeWorkItemPriority;
using AppSupportHub.Application.WorkItems.CreateWorkItem;
using AppSupportHub.Application.WorkItems.Inputs;
using AppSupportHub.Application.WorkItems.ListWorkItems;
using AppSupportHub.Application.WorkItems.ReadModels;
using AppSupportHub.Application.WorkItems.TransitionWorkItemStatus;
using AppSupportHub.Web.Http;

namespace AppSupportHub.Web.DemoData;

public sealed class DemoDataSeeder(
    ApplicationSystemInputFactory systemInputFactory,
    WorkItemInputFactory workItemInputFactory,
    ListApplicationSystemsHandler listSystemsHandler,
    CreateApplicationSystemHandler createSystemHandler,
    ListWorkItemsHandler listWorkItemsHandler,
    CreateWorkItemHandler createWorkItemHandler,
    AssignWorkItemHandler assignHandler,
    ChangeWorkItemDueDateHandler dueDateHandler,
    ChangeWorkItemPriorityHandler priorityHandler,
    TransitionWorkItemStatusHandler transitionHandler,
    TimeProvider timeProvider)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        DemoSystem[] systems =
        {
            new DemoSystem(
                "Aurora Ledger Sandbox",
                "Fictional commercial ledger sandbox used only for portfolio demonstrations.",
                "Commercial",
                "High",
                "Active",
                "Fictional Finance Practice",
                "Demo Platform Owner",
                "Portfolio Support Lab",
                "Northstar Demo Software"),
            new DemoSystem(
                "Nimbus Intake Lab",
                "Fictional custom intake workflow for safe demonstration records.",
                "Custom",
                "Medium",
                "Active",
                "Fictional Service Practice",
                "Demo Product Owner",
                "Portfolio Support Lab",
                null),
            new DemoSystem(
                "Orchid Archive Prototype",
                "Fictional planned archive prototype with no production connection.",
                "Custom",
                "Low",
                "Planned",
                "Fictional Records Practice",
                "Demo Architecture Owner",
                "Portfolio Support Lab",
                null),
        };

        var systemIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (DemoSystem system in systems)
        {
            systemIds[system.Name] = await EnsureSystemAsync(system, cancellationToken);
        }

        DateTimeOffset tomorrow = timeProvider.GetUtcNow().AddDays(1);
        await EnsureWorkItemAsync(
            systemIds[systems[0].Name],
            "Incident",
            "Investigate synthetic export delay",
            "Review a fictional delayed export in the isolated demonstration workflow.",
            "High",
            null,
            async id =>
            {
                Require(await assignHandler.ExecuteAsync(
                    new AssignWorkItemCommand(
                        id,
                        "demo.support.specialist@appsupporthub.local",
                        DemoActor.Identifier),
                    cancellationToken));
                Require(await dueDateHandler.ExecuteAsync(
                    new ChangeWorkItemDueDateCommand(id, tomorrow, DemoActor.Identifier),
                    cancellationToken));
                Require(await priorityHandler.ExecuteAsync(
                    Require(workItemInputFactory.CreatePriorityCommand(
                        id,
                        "Critical",
                        DemoActor.Identifier)),
                    cancellationToken));
                Require(await transitionHandler.ExecuteAsync(
                    Require(workItemInputFactory.CreateTransitionCommand(
                        id,
                        "UnderAnalysis",
                        DemoActor.Identifier,
                        "Synthetic triage started.",
                        null)),
                    cancellationToken));
                Require(await transitionHandler.ExecuteAsync(
                    Require(workItemInputFactory.CreateTransitionCommand(
                        id,
                        "InProgress",
                        DemoActor.Identifier,
                        "Fictional investigation is underway.",
                        null)),
                    cancellationToken));
            },
            cancellationToken);
        await EnsureWorkItemAsync(
            systemIds[systems[0].Name],
            "Enhancement",
            "Add fictional audit dashboard",
            "Design a demonstration-only dashboard for synthetic audit events.",
            "Medium",
            tomorrow.AddDays(10),
            null,
            cancellationToken);
        await EnsureWorkItemAsync(
            systemIds[systems[1].Name],
            "ChangeRequest",
            "Rotate demo integration certificate",
            "Plan a fictional certificate rotation without any external connection.",
            "High",
            tomorrow.AddDays(5),
            null,
            cancellationToken);
        await EnsureWorkItemAsync(
            systemIds[systems[1].Name],
            "Enhancement",
            "Clarify sample intake guidance",
            "Improve fictional help text used in the portfolio demonstration.",
            "Low",
            null,
            null,
            cancellationToken);
        await EnsureWorkItemAsync(
            systemIds[systems[2].Name],
            "Incident",
            "Review prototype retention toggle",
            "Inspect a synthetic retention toggle in the planned prototype.",
            "Medium",
            null,
            null,
            cancellationToken);
    }

    private async Task<Guid> EnsureSystemAsync(
        DemoSystem system,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationSystemSummary> existing = Require(
            await listSystemsHandler.ExecuteAsync(
                Require(systemInputFactory.CreateListQuery(system.Name, null, null, null, 100)),
                cancellationToken));
        ApplicationSystemSummary? match = existing.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, system.Name, StringComparison.Ordinal));
        if (match is not null)
        {
            return match.Id;
        }

        CreatedApplicationSystem created = Require(await createSystemHandler.ExecuteAsync(
            Require(systemInputFactory.CreateCreateCommand(
                system.Name,
                system.Description,
                system.Type,
                system.Criticality,
                system.InitialLifecycleStatus,
                system.BusinessOwner,
                system.TechnicalOwner,
                system.SupportTeam,
                system.VendorName)),
            cancellationToken));
        return created.Id;
    }

    private async Task EnsureWorkItemAsync(
        Guid applicationSystemId,
        string type,
        string title,
        string description,
        string priority,
        DateTimeOffset? dueAt,
        Func<Guid, Task>? configure,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkItemSummary> existing = Require(
            await listWorkItemsHandler.ExecuteAsync(
                Require(workItemInputFactory.CreateListQuery(
                    applicationSystemId,
                    title,
                    null,
                    null,
                    null,
                    null,
                    false,
                    100)),
                cancellationToken));
        if (existing.Any(candidate => string.Equals(
            candidate.Title,
            title,
            StringComparison.Ordinal)))
        {
            return;
        }

        CreatedWorkItem created = Require(await createWorkItemHandler.ExecuteAsync(
            Require(workItemInputFactory.CreateCreateCommand(
                applicationSystemId,
                type,
                title,
                description,
                priority,
                dueAt,
                DemoActor.Identifier)),
            cancellationToken));
        if (configure is not null)
        {
            await configure(created.Id);
        }
    }

    private static T Require<T>(ApplicationResult<T> result)
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Demo data seeding failed: {result.Error!.Code}");
        }

        return result.Value;
    }

    private sealed record DemoSystem(
        string Name,
        string Description,
        string Type,
        string Criticality,
        string InitialLifecycleStatus,
        string BusinessOwner,
        string TechnicalOwner,
        string SupportTeam,
        string? VendorName);
}
