using AppSupportHub.Domain.Systems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppSupportHub.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationSystemConfiguration : IEntityTypeConfiguration<ApplicationSystem>
{
    public void Configure(EntityTypeBuilder<ApplicationSystem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("application_systems", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_application_systems_required_text_trimmed",
                "name::text = btrim(name::text) AND name::text <> '' "
                + "AND description = btrim(description) AND description <> '' "
                + "AND business_owner = btrim(business_owner) AND business_owner <> '' "
                + "AND technical_owner = btrim(technical_owner) AND technical_owner <> '' "
                + "AND support_team = btrim(support_team) AND support_team <> ''");
            tableBuilder.HasCheckConstraint(
                "ck_application_systems_name_length",
                $"char_length(name::text) <= {ApplicationSystem.NameMaxLength}");
            tableBuilder.HasCheckConstraint(
                "ck_application_systems_timestamp_order",
                "updated_at_utc >= created_at_utc");
            tableBuilder.HasCheckConstraint(
                "ck_application_systems_commercial_vendor",
                "\"type\" <> 'Commercial' "
                + "OR (vendor_name IS NOT NULL AND vendor_name = btrim(vendor_name) "
                + "AND vendor_name <> '')");
            tableBuilder.HasCheckConstraint(
                "ck_application_systems_retirement_state",
                "(lifecycle_status = 'Retired' AND retired_at_utc IS NOT NULL "
                + "AND retirement_reason IS NOT NULL AND retirement_reason = btrim(retirement_reason) "
                + "AND retirement_reason <> '') OR (lifecycle_status <> 'Retired' "
                + "AND retired_at_utc IS NULL AND retirement_reason IS NULL)");
            tableBuilder.HasCheckConstraint(
                "ck_application_systems_type",
                "\"type\" IN ('Commercial', 'Custom')");
            tableBuilder.HasCheckConstraint(
                "ck_application_systems_criticality",
                "criticality IN ('Low', 'Medium', 'High', 'Critical')");
            tableBuilder.HasCheckConstraint(
                "ck_application_systems_lifecycle_status",
                "lifecycle_status IN ('Planned', 'Active', 'Maintenance', 'Retired')");
        });

        builder.HasKey(applicationSystem => applicationSystem.Id)
            .HasName("pk_application_systems");

        builder.Property(applicationSystem => applicationSystem.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(applicationSystem => applicationSystem.Name)
            .HasColumnName("name")
            .HasColumnType("citext")
            .HasMaxLength(ApplicationSystem.NameMaxLength)
            .IsRequired();
        builder.Property(applicationSystem => applicationSystem.Description)
            .HasColumnName("description")
            .HasMaxLength(ApplicationSystem.DescriptionMaxLength)
            .IsRequired();
        builder.Property(applicationSystem => applicationSystem.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(applicationSystem => applicationSystem.Criticality)
            .HasColumnName("criticality")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(applicationSystem => applicationSystem.LifecycleStatus)
            .HasColumnName("lifecycle_status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(applicationSystem => applicationSystem.BusinessOwner)
            .HasColumnName("business_owner")
            .HasMaxLength(ApplicationSystem.BusinessOwnerMaxLength)
            .IsRequired();
        builder.Property(applicationSystem => applicationSystem.TechnicalOwner)
            .HasColumnName("technical_owner")
            .HasMaxLength(ApplicationSystem.TechnicalOwnerMaxLength)
            .IsRequired();
        builder.Property(applicationSystem => applicationSystem.SupportTeam)
            .HasColumnName("support_team")
            .HasMaxLength(ApplicationSystem.SupportTeamMaxLength)
            .IsRequired();
        builder.Property(applicationSystem => applicationSystem.VendorName)
            .HasColumnName("vendor_name")
            .HasMaxLength(ApplicationSystem.VendorNameMaxLength);
        builder.Property(applicationSystem => applicationSystem.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(applicationSystem => applicationSystem.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(applicationSystem => applicationSystem.RetiredAtUtc)
            .HasColumnName("retired_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(applicationSystem => applicationSystem.RetirementReason)
            .HasColumnName("retirement_reason")
            .HasMaxLength(ApplicationSystem.RetirementReasonMaxLength);
        builder.Property<uint>(AppSupportHubDbContext.VersionPropertyName)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.Ignore(applicationSystem => applicationSystem.IsRetired);

        builder.HasIndex(applicationSystem => applicationSystem.Name)
            .IsUnique()
            .HasDatabaseName("ux_application_systems_name");
        builder.HasIndex(applicationSystem => applicationSystem.LifecycleStatus)
            .HasDatabaseName("ix_application_systems_lifecycle_status");
        builder.HasIndex(applicationSystem => applicationSystem.Criticality)
            .HasDatabaseName("ix_application_systems_criticality");
        builder.HasIndex(applicationSystem => applicationSystem.SupportTeam)
            .HasDatabaseName("ix_application_systems_support_team");
    }
}
