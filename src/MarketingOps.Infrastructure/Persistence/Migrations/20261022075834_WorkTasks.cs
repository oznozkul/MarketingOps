using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketingOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "last_task_number",
                table: "sites",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "task_key_prefix",
                table: "sites",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "task_tracker_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    project_key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    issue_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    protected_api_token = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_tracker_connections", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_tracker_connections_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    finding_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    key = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    problem = table.Column<string>(type: "text", nullable: false),
                    impact = table.Column<string>(type: "text", nullable: false),
                    suggestion = table.Column<string>(type: "jsonb", nullable: false),
                    assigned_team = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    external_tracker_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    external_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "jsonb", nullable: false),
                    criteria = table.Column<string>(type: "jsonb", nullable: false),
                    feedback = table.Column<string>(type: "jsonb", nullable: false),
                    history = table.Column<string>(type: "jsonb", nullable: false),
                    references = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_tasks_findings_finding_id",
                        column: x => x.finding_id,
                        principalTable: "findings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_work_tasks_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_task_tracker_connections_site_id",
                table: "task_tracker_connections",
                column: "site_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_tasks_finding_id",
                table: "work_tasks",
                column: "finding_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_tasks_site_id_number",
                table: "work_tasks",
                columns: new[] { "site_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_tasks_site_id_status",
                table: "work_tasks",
                columns: new[] { "site_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_tracker_connections");

            migrationBuilder.DropTable(
                name: "work_tasks");

            migrationBuilder.DropColumn(
                name: "last_task_number",
                table: "sites");

            migrationBuilder.DropColumn(
                name: "task_key_prefix",
                table: "sites");
        }
    }
}
