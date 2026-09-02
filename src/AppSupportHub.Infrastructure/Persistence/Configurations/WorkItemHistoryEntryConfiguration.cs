using AppSupportHub.Domain.WorkItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppSupportHub.Infrastructure.Persistence.Configurations;

internal sealed class WorkItemHistoryEntryConfiguration
    : IEntityTypeConfiguration<WorkItemHistoryEntry>
{
    public void Configure(EntityTypeBuilder<WorkItemHistoryEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("work_item_history_entries", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_work_item_history_entries_sequence",
                "sequence > 0");
            tableBuilder.HasCheckConstraint(
                "ck_work_item_history_entries_actor_trimmed",
                "actor_identifier = btrim(actor_identifier) AND actor_identifier <> ''");
            tableBuilder.HasCheckConstraint(
                "ck_work_item_history_entries_event_type",
                "event_type IN ('Created', 'DetailsUpdated', 'Assigned', 'Unassigned', "
                + "'PriorityChanged', 'DueDateChanged', 'StatusChanged', "
                + "'ResolutionRecorded', 'Reopened', 'Cancelled')");
        });

        builder.HasKey(historyEntry => historyEntry.Id)
            .HasName("pk_work_item_history_entries");

        builder.Property(historyEntry => historyEntry.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(historyEntry => historyEntry.WorkItemId)
            .HasColumnName("work_item_id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property<int>(AppSupportHubDbContext.HistorySequencePropertyName)
            .HasColumnName("sequence")
            .IsRequired();
        builder.Property(historyEntry => historyEntry.EventType)
            .HasColumnName("event_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(historyEntry => historyEntry.ActorIdentifier)
            .HasColumnName("actor_identifier")
            .HasMaxLength(WorkItem.ActorIdentifierMaxLength)
            .IsRequired();
        builder.Property(historyEntry => historyEntry.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(historyEntry => historyEntry.PreviousValue)
            .HasColumnName("previous_value")
            .HasMaxLength(WorkItem.HistoryValueMaxLength);
        builder.Property(historyEntry => historyEntry.NewValue)
            .HasColumnName("new_value")
            .HasMaxLength(WorkItem.HistoryValueMaxLength);
        builder.Property(historyEntry => historyEntry.Comment)
            .HasColumnName("comment")
            .HasMaxLength(WorkItem.HistoryCommentMaxLength);

        builder.HasIndex(
                nameof(WorkItemHistoryEntry.WorkItemId),
                AppSupportHubDbContext.HistorySequencePropertyName)
            .IsUnique()
            .HasDatabaseName("ux_work_item_history_entries_work_item_id_sequence");
    }
}
