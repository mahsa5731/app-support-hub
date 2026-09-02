using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppSupportHub.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialPostgreSqlPersistence : Migration
{
    private static readonly string[] _historySequenceColumns = ["work_item_id", "sequence"];
    private static readonly string[] _workItemStatusColumns = ["application_system_id", "status"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:citext", ",,");

        migrationBuilder.CreateTable(
            name: "application_systems",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "citext", maxLength: 150, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                criticality = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                lifecycle_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                business_owner = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                technical_owner = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                support_team = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                vendor_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                retired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                retirement_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_application_systems", x => x.id);
                table.CheckConstraint("ck_application_systems_commercial_vendor", "\"type\" <> 'Commercial' OR (vendor_name IS NOT NULL AND vendor_name = btrim(vendor_name) AND vendor_name <> '')");
                table.CheckConstraint("ck_application_systems_criticality", "criticality IN ('Low', 'Medium', 'High', 'Critical')");
                table.CheckConstraint("ck_application_systems_lifecycle_status", "lifecycle_status IN ('Planned', 'Active', 'Maintenance', 'Retired')");
                table.CheckConstraint("ck_application_systems_name_length", "char_length(name::text) <= 150");
                table.CheckConstraint("ck_application_systems_required_text_trimmed", "name::text = btrim(name::text) AND name::text <> '' AND description = btrim(description) AND description <> '' AND business_owner = btrim(business_owner) AND business_owner <> '' AND technical_owner = btrim(technical_owner) AND technical_owner <> '' AND support_team = btrim(support_team) AND support_team <> ''");
                table.CheckConstraint("ck_application_systems_retirement_state", "(lifecycle_status = 'Retired' AND retired_at_utc IS NOT NULL AND retirement_reason IS NOT NULL AND retirement_reason = btrim(retirement_reason) AND retirement_reason <> '') OR (lifecycle_status <> 'Retired' AND retired_at_utc IS NULL AND retirement_reason IS NULL)");
                table.CheckConstraint("ck_application_systems_timestamp_order", "updated_at_utc >= created_at_utc");
                table.CheckConstraint("ck_application_systems_type", "\"type\" IN ('Commercial', 'Custom')");
            });

        migrationBuilder.CreateTable(
            name: "work_items",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                application_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                assignee_identifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                due_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                resolution_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_work_items", x => x.id);
                table.CheckConstraint("ck_work_items_due_date", "due_at_utc IS NULL OR due_at_utc > created_at_utc");
                table.CheckConstraint("ck_work_items_priority", "priority IN ('Low', 'Medium', 'High', 'Critical')");
                table.CheckConstraint("ck_work_items_required_text_trimmed", "title = btrim(title) AND title <> '' AND description = btrim(description) AND description <> ''");
                table.CheckConstraint("ck_work_items_resolution_state", "(status IN ('Resolved', 'Closed') AND resolution_summary IS NOT NULL AND resolution_summary = btrim(resolution_summary) AND resolution_summary <> '' AND resolved_at_utc IS NOT NULL) OR (status NOT IN ('Resolved', 'Closed') AND resolution_summary IS NULL AND resolved_at_utc IS NULL)");
                table.CheckConstraint("ck_work_items_status", "status IN ('New', 'UnderAnalysis', 'InProgress', 'Blocked', 'Testing', 'Resolved', 'Closed', 'Cancelled')");
                table.CheckConstraint("ck_work_items_timestamp_order", "updated_at_utc >= created_at_utc");
                table.CheckConstraint("ck_work_items_type", "\"type\" IN ('Incident', 'Enhancement', 'ChangeRequest')");
                table.ForeignKey(
                    name: "fk_work_items_application_systems_application_system_id",
                    column: x => x.application_system_id,
                    principalTable: "application_systems",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "work_item_history_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                event_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                actor_identifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                previous_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                new_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                sequence = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_work_item_history_entries", x => x.id);
                table.CheckConstraint("ck_work_item_history_entries_actor_trimmed", "actor_identifier = btrim(actor_identifier) AND actor_identifier <> ''");
                table.CheckConstraint("ck_work_item_history_entries_event_type", "event_type IN ('Created', 'DetailsUpdated', 'Assigned', 'Unassigned', 'PriorityChanged', 'DueDateChanged', 'StatusChanged', 'ResolutionRecorded', 'Reopened', 'Cancelled')");
                table.CheckConstraint("ck_work_item_history_entries_sequence", "sequence > 0");
                table.ForeignKey(
                    name: "fk_work_item_history_entries_work_items_work_item_id",
                    column: x => x.work_item_id,
                    principalTable: "work_items",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_application_systems_criticality",
            table: "application_systems",
            column: "criticality");

        migrationBuilder.CreateIndex(
            name: "ix_application_systems_lifecycle_status",
            table: "application_systems",
            column: "lifecycle_status");

        migrationBuilder.CreateIndex(
            name: "ix_application_systems_support_team",
            table: "application_systems",
            column: "support_team");

        migrationBuilder.CreateIndex(
            name: "ux_application_systems_name",
            table: "application_systems",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_work_item_history_entries_work_item_id_sequence",
            table: "work_item_history_entries",
            columns: _historySequenceColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_work_items_application_system_id",
            table: "work_items",
            column: "application_system_id");

        migrationBuilder.CreateIndex(
            name: "ix_work_items_application_system_id_status",
            table: "work_items",
            columns: _workItemStatusColumns);

        migrationBuilder.CreateIndex(
            name: "ix_work_items_due_at_utc",
            table: "work_items",
            column: "due_at_utc");

        migrationBuilder.CreateIndex(
            name: "ix_work_items_priority",
            table: "work_items",
            column: "priority");

        migrationBuilder.CreateIndex(
            name: "ix_work_items_status",
            table: "work_items",
            column: "status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "work_item_history_entries");

        migrationBuilder.DropTable(
            name: "work_items");

        migrationBuilder.DropTable(
            name: "application_systems");
    }
}
