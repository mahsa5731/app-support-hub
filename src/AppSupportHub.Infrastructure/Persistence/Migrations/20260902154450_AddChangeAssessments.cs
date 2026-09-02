using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppSupportHub.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddChangeAssessments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "change_assessments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                business_need = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                technical_impact = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                security_impact = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                risk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                acceptance_criteria = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                test_plan = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                rollback_plan = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                assessed_by_identifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_change_assessments", x => x.id);
                table.CheckConstraint("ck_change_assessments_assessed_by_length", "char_length(assessed_by_identifier) <= 200");
                table.CheckConstraint("ck_change_assessments_narrative_lengths", "char_length(business_need) <= 2000 AND char_length(technical_impact) <= 2000 AND char_length(security_impact) <= 2000 AND char_length(acceptance_criteria) <= 2000 AND char_length(test_plan) <= 2000 AND char_length(rollback_plan) <= 2000");
                table.CheckConstraint("ck_change_assessments_required_text_trimmed", "business_need = btrim(business_need) AND business_need <> '' AND technical_impact = btrim(technical_impact) AND technical_impact <> '' AND security_impact = btrim(security_impact) AND security_impact <> '' AND acceptance_criteria = btrim(acceptance_criteria) AND acceptance_criteria <> '' AND test_plan = btrim(test_plan) AND test_plan <> '' AND rollback_plan = btrim(rollback_plan) AND rollback_plan <> '' AND assessed_by_identifier = btrim(assessed_by_identifier) AND assessed_by_identifier <> ''");
                table.CheckConstraint("ck_change_assessments_risk", "risk IN ('Low', 'Medium', 'High', 'Critical')");
                table.CheckConstraint("ck_change_assessments_timestamp_order", "updated_at_utc >= created_at_utc");
                table.ForeignKey(
                    name: "fk_change_assessments_work_items_work_item_id",
                    column: x => x.work_item_id,
                    principalTable: "work_items",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ux_change_assessments_work_item_id",
            table: "change_assessments",
            column: "work_item_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "change_assessments");
    }
}
