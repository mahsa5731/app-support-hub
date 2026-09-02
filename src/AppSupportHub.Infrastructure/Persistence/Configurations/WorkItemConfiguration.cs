using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppSupportHub.Infrastructure.Persistence.Configurations;

internal sealed class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("work_items", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_work_items_required_text_trimmed",
                "title = btrim(title) AND title <> '' "
                + "AND description = btrim(description) AND description <> ''");
            tableBuilder.HasCheckConstraint(
                "ck_work_items_timestamp_order",
                "updated_at_utc >= created_at_utc");
            tableBuilder.HasCheckConstraint(
                "ck_work_items_due_date",
                "due_at_utc IS NULL OR due_at_utc > created_at_utc");
            tableBuilder.HasCheckConstraint(
                "ck_work_items_resolution_state",
                "(status IN ('Resolved', 'Closed') AND resolution_summary IS NOT NULL "
                + "AND resolution_summary = btrim(resolution_summary) "
                + "AND resolution_summary <> '' AND resolved_at_utc IS NOT NULL) "
                + "OR (status NOT IN ('Resolved', 'Closed') "
                + "AND resolution_summary IS NULL AND resolved_at_utc IS NULL)");
            tableBuilder.HasCheckConstraint(
                "ck_work_items_type",
                "\"type\" IN ('Incident', 'Enhancement', 'ChangeRequest')");
            tableBuilder.HasCheckConstraint(
                "ck_work_items_priority",
                "priority IN ('Low', 'Medium', 'High', 'Critical')");
            tableBuilder.HasCheckConstraint(
                "ck_work_items_status",
                "status IN ('New', 'UnderAnalysis', 'InProgress', 'Blocked', "
                + "'Testing', 'Resolved', 'Closed', 'Cancelled')");
        });

        builder.HasKey(workItem => workItem.Id)
            .HasName("pk_work_items");

        builder.Property(workItem => workItem.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(workItem => workItem.ApplicationSystemId)
            .HasColumnName("application_system_id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(workItem => workItem.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(workItem => workItem.Title)
            .HasColumnName("title")
            .HasMaxLength(WorkItem.TitleMaxLength)
            .IsRequired();
        builder.Property(workItem => workItem.Description)
            .HasColumnName("description")
            .HasMaxLength(WorkItem.DescriptionMaxLength)
            .IsRequired();
        builder.Property(workItem => workItem.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(workItem => workItem.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(workItem => workItem.AssigneeIdentifier)
            .HasColumnName("assignee_identifier")
            .HasMaxLength(WorkItem.AssigneeIdentifierMaxLength);
        builder.Property(workItem => workItem.DueAtUtc)
            .HasColumnName("due_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(workItem => workItem.ResolutionSummary)
            .HasColumnName("resolution_summary")
            .HasMaxLength(WorkItem.ResolutionSummaryMaxLength);
        builder.Property(workItem => workItem.ResolvedAtUtc)
            .HasColumnName("resolved_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(workItem => workItem.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(workItem => workItem.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property<uint>(AppSupportHubDbContext.VersionPropertyName)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasOne<ApplicationSystem>()
            .WithMany()
            .HasForeignKey(workItem => workItem.ApplicationSystemId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_work_items_application_systems_application_system_id");
        builder.HasMany(workItem => workItem.History)
            .WithOne()
            .HasForeignKey(historyEntry => historyEntry.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_work_item_history_entries_work_items_work_item_id");
        builder.Navigation(workItem => workItem.History)
            .HasField("_history")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(workItem => workItem.ApplicationSystemId)
            .HasDatabaseName("ix_work_items_application_system_id");
        builder.HasIndex(workItem => workItem.Status)
            .HasDatabaseName("ix_work_items_status");
        builder.HasIndex(workItem => workItem.Priority)
            .HasDatabaseName("ix_work_items_priority");
        builder.HasIndex(workItem => workItem.DueAtUtc)
            .HasDatabaseName("ix_work_items_due_at_utc");
        builder.HasIndex(workItem => new { workItem.ApplicationSystemId, workItem.Status })
            .HasDatabaseName("ix_work_items_application_system_id_status");
    }
}
