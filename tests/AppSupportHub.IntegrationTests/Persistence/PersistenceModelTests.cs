using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AppSupportHub.IntegrationTests.Persistence;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class PersistenceModelTests(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public void ModelMapsTablesColumnsLengthsAndEnumStrings()
    {
        using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        IEntityType applicationSystem = GetEntityType<ApplicationSystem>(dbContext);
        IEntityType workItem = GetEntityType<WorkItem>(dbContext);
        IEntityType historyEntry = GetEntityType<WorkItemHistoryEntry>(dbContext);

        Assert.Equal("application_systems", applicationSystem.GetTableName());
        Assert.Equal("work_items", workItem.GetTableName());
        Assert.Equal("work_item_history_entries", historyEntry.GetTableName());

        AssertProperty(
            applicationSystem,
            nameof(ApplicationSystem.Name),
            "name",
            ApplicationSystem.NameMaxLength);
        Assert.Equal("citext", GetProperty(applicationSystem, nameof(ApplicationSystem.Name)).GetColumnType());
        AssertProperty(
            applicationSystem,
            nameof(ApplicationSystem.Description),
            "description",
            ApplicationSystem.DescriptionMaxLength);
        AssertProperty(
            workItem,
            nameof(WorkItem.Title),
            "title",
            WorkItem.TitleMaxLength);
        AssertProperty(
            workItem,
            nameof(WorkItem.Description),
            "description",
            WorkItem.DescriptionMaxLength);
        AssertProperty(
            historyEntry,
            nameof(WorkItemHistoryEntry.ActorIdentifier),
            "actor_identifier",
            WorkItem.ActorIdentifierMaxLength);
        AssertProperty(
            historyEntry,
            "Sequence",
            "sequence",
            null);

        AssertEnumString(applicationSystem, nameof(ApplicationSystem.Type));
        AssertEnumString(applicationSystem, nameof(ApplicationSystem.Criticality));
        AssertEnumString(applicationSystem, nameof(ApplicationSystem.LifecycleStatus));
        AssertEnumString(workItem, nameof(WorkItem.Type));
        AssertEnumString(workItem, nameof(WorkItem.Priority));
        AssertEnumString(workItem, nameof(WorkItem.Status));
        AssertEnumString(historyEntry, nameof(WorkItemHistoryEntry.EventType));
    }

    [Fact]
    public void ModelMapsRelationshipsIndexesHistoryFieldAndConcurrency()
    {
        using AppSupportHubDbContext dbContext = fixture.CreateDbContext();
        IEntityType applicationSystem = GetEntityType<ApplicationSystem>(dbContext);
        IEntityType workItem = GetEntityType<WorkItem>(dbContext);
        IEntityType historyEntry = GetEntityType<WorkItemHistoryEntry>(dbContext);

        IForeignKey systemForeignKey = Assert.Single(workItem.GetForeignKeys());
        Assert.Equal(DeleteBehavior.Restrict, systemForeignKey.DeleteBehavior);
        Assert.Equal(applicationSystem, systemForeignKey.PrincipalEntityType);

        IForeignKey historyForeignKey = Assert.Single(historyEntry.GetForeignKeys());
        Assert.Equal(DeleteBehavior.Cascade, historyForeignKey.DeleteBehavior);
        Assert.Equal(workItem, historyForeignKey.PrincipalEntityType);

        INavigation historyNavigation = Assert.IsAssignableFrom<INavigation>(
            workItem.FindNavigation(nameof(WorkItem.History)));
        Assert.Equal(PropertyAccessMode.Field, historyNavigation.GetPropertyAccessMode());
        Assert.Equal("_history", historyNavigation.FieldInfo?.Name);

        AssertConcurrencyToken(applicationSystem);
        AssertConcurrencyToken(workItem);

        string[] systemIndexes = applicationSystem.GetIndexes()
            .Select(index => index.GetDatabaseName())
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Contains("ux_application_systems_name", systemIndexes);
        Assert.Contains("ix_application_systems_lifecycle_status", systemIndexes);
        Assert.Contains("ix_application_systems_criticality", systemIndexes);
        Assert.Contains("ix_application_systems_support_team", systemIndexes);

        IIndex sequenceIndex = Assert.Single(
            historyEntry.GetIndexes(),
            index => index.GetDatabaseName()
                == "ux_work_item_history_entries_work_item_id_sequence");
        Assert.True(sequenceIndex.IsUnique);
        Assert.Equal(
            [nameof(WorkItemHistoryEntry.WorkItemId), "Sequence"],
            sequenceIndex.Properties.Select(property => property.Name));

        string[] workItemIndexes = workItem.GetIndexes()
            .Select(index => index.GetDatabaseName())
            .OfType<string>()
            .ToArray();
        Assert.Contains("ix_work_items_application_system_id", workItemIndexes);
        Assert.Contains("ix_work_items_status", workItemIndexes);
        Assert.Contains("ix_work_items_priority", workItemIndexes);
        Assert.Contains("ix_work_items_due_at_utc", workItemIndexes);
        Assert.Contains("ix_work_items_application_system_id_status", workItemIndexes);
    }

    private static IEntityType GetEntityType<TEntity>(AppSupportHubDbContext dbContext)
    {
        return Assert.IsAssignableFrom<IEntityType>(dbContext.Model.FindEntityType(typeof(TEntity)));
    }

    private static IProperty GetProperty(IEntityType entityType, string propertyName)
    {
        return Assert.IsAssignableFrom<IProperty>(entityType.FindProperty(propertyName));
    }

    private static void AssertProperty(
        IEntityType entityType,
        string propertyName,
        string columnName,
        int? maximumLength)
    {
        IProperty property = GetProperty(entityType, propertyName);
        Assert.Equal(columnName, property.GetColumnName());
        Assert.Equal(maximumLength, property.GetMaxLength());
    }

    private static void AssertEnumString(IEntityType entityType, string propertyName)
    {
        IProperty property = GetProperty(entityType, propertyName);
        Assert.Equal(typeof(string), property.GetTypeMapping().Converter?.ProviderClrType);
    }

    private static void AssertConcurrencyToken(IEntityType entityType)
    {
        IProperty property = GetProperty(entityType, "Version");
        Assert.True(property.IsShadowProperty());
        Assert.True(property.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
        Assert.Equal("xmin", property.GetColumnName());
    }
}
