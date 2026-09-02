using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Domain.WorkItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AppSupportHub.Infrastructure.Persistence;

public sealed class AppSupportHubDbContext(
    DbContextOptions<AppSupportHubDbContext> options) : DbContext(options), IUnitOfWork
{
    internal const string HistorySequencePropertyName = "Sequence";
    internal const string VersionPropertyName = "Version";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppSupportHubDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareHistoryEntries();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        PrepareHistoryEntries();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareHistoryEntries()
    {
        ChangeTracker.DetectChanges();

        var historyEntries =
            ChangeTracker.Entries<WorkItemHistoryEntry>().ToList();

        if (historyEntries.Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "Existing work-item history entries are append-only and cannot be modified or deleted.");
        }

        var addedEntries =
            historyEntries
                .Where(entry => entry.State == EntityState.Added)
                .ToDictionary(entry => entry.Entity);

        if (addedEntries.Count == 0)
        {
            return;
        }

        foreach (EntityEntry<WorkItem> workItemEntry in ChangeTracker.Entries<WorkItem>())
        {
            int nextSequence = historyEntries
                .Where(entry => entry.Entity.WorkItemId == workItemEntry.Entity.Id)
                .Select(entry => entry.Property<int>(HistorySequencePropertyName).CurrentValue)
                .DefaultIfEmpty()
                .Max() + 1;

            foreach (WorkItemHistoryEntry historyEntry in workItemEntry.Entity.History)
            {
                if (!addedEntries.TryGetValue(historyEntry, out EntityEntry<WorkItemHistoryEntry>? entry))
                {
                    continue;
                }

                PropertyEntry<WorkItemHistoryEntry, int> sequenceProperty =
                    entry.Property<int>(HistorySequencePropertyName);

                if (sequenceProperty.CurrentValue == 0)
                {
                    sequenceProperty.CurrentValue = nextSequence;
                }

                nextSequence = Math.Max(nextSequence, sequenceProperty.CurrentValue + 1);
            }
        }
    }
}
