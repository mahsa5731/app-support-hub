using AppSupportHub.Domain.ChangeAssessments;
using AppSupportHub.Domain.WorkItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppSupportHub.Infrastructure.Persistence.Configurations;

internal sealed class ChangeAssessmentConfiguration
    : IEntityTypeConfiguration<ChangeAssessment>
{
    public void Configure(EntityTypeBuilder<ChangeAssessment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("change_assessments", table =>
        {
            table.HasCheckConstraint(
                "ck_change_assessments_required_text_trimmed",
                "business_need = btrim(business_need) AND business_need <> '' "
                + "AND technical_impact = btrim(technical_impact) AND technical_impact <> '' "
                + "AND security_impact = btrim(security_impact) AND security_impact <> '' "
                + "AND acceptance_criteria = btrim(acceptance_criteria) "
                + "AND acceptance_criteria <> '' AND test_plan = btrim(test_plan) "
                + "AND test_plan <> '' AND rollback_plan = btrim(rollback_plan) "
                + "AND rollback_plan <> '' AND assessed_by_identifier = "
                + "btrim(assessed_by_identifier) AND assessed_by_identifier <> ''");
            table.HasCheckConstraint(
                "ck_change_assessments_narrative_lengths",
                $"char_length(business_need) <= {ChangeAssessment.NarrativeMaxLength} "
                + $"AND char_length(technical_impact) <= {ChangeAssessment.NarrativeMaxLength} "
                + $"AND char_length(security_impact) <= {ChangeAssessment.NarrativeMaxLength} "
                + $"AND char_length(acceptance_criteria) <= {ChangeAssessment.NarrativeMaxLength} "
                + $"AND char_length(test_plan) <= {ChangeAssessment.NarrativeMaxLength} "
                + $"AND char_length(rollback_plan) <= {ChangeAssessment.NarrativeMaxLength}");
            table.HasCheckConstraint(
                "ck_change_assessments_assessed_by_length",
                $"char_length(assessed_by_identifier) <= {ChangeAssessment.AssessedByMaxLength}");
            table.HasCheckConstraint(
                "ck_change_assessments_risk",
                "risk IN ('Low', 'Medium', 'High', 'Critical')");
            table.HasCheckConstraint(
                "ck_change_assessments_timestamp_order",
                "updated_at_utc >= created_at_utc");
        });

        builder.HasKey(assessment => assessment.Id).HasName("pk_change_assessments");
        builder.Property(assessment => assessment.Id)
            .HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(assessment => assessment.WorkItemId)
            .HasColumnName("work_item_id").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(assessment => assessment.BusinessNeed)
            .HasColumnName("business_need").HasMaxLength(ChangeAssessment.NarrativeMaxLength)
            .IsRequired();
        builder.Property(assessment => assessment.TechnicalImpact)
            .HasColumnName("technical_impact").HasMaxLength(ChangeAssessment.NarrativeMaxLength)
            .IsRequired();
        builder.Property(assessment => assessment.SecurityImpact)
            .HasColumnName("security_impact").HasMaxLength(ChangeAssessment.NarrativeMaxLength)
            .IsRequired();
        builder.Property(assessment => assessment.Risk)
            .HasColumnName("risk").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(assessment => assessment.AcceptanceCriteria)
            .HasColumnName("acceptance_criteria").HasMaxLength(ChangeAssessment.NarrativeMaxLength)
            .IsRequired();
        builder.Property(assessment => assessment.TestPlan)
            .HasColumnName("test_plan").HasMaxLength(ChangeAssessment.NarrativeMaxLength)
            .IsRequired();
        builder.Property(assessment => assessment.RollbackPlan)
            .HasColumnName("rollback_plan").HasMaxLength(ChangeAssessment.NarrativeMaxLength)
            .IsRequired();
        builder.Property(assessment => assessment.AssessedByIdentifier)
            .HasColumnName("assessed_by_identifier")
            .HasMaxLength(ChangeAssessment.AssessedByMaxLength).IsRequired();
        builder.Property(assessment => assessment.CreatedAtUtc)
            .HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(assessment => assessment.UpdatedAtUtc)
            .HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").IsRequired();

        builder.HasOne<WorkItem>().WithOne()
            .HasForeignKey<ChangeAssessment>(assessment => assessment.WorkItemId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_change_assessments_work_items_work_item_id");
        builder.HasIndex(assessment => assessment.WorkItemId)
            .IsUnique()
            .HasDatabaseName("ux_change_assessments_work_item_id");
    }
}
