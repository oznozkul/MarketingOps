using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketingOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Findings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "in_sitemap",
                table: "page_snapshots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "asset_checks",
                table: "crawls",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "evaluated_at",
                table: "crawls",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "new_findings",
                table: "crawls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "reopened_findings",
                table: "crawls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "resolved_findings",
                table: "crawls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "findings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    page_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    snoozed_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    false_positive_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    affected_count = table.Column<int>(type: "integer", nullable: false),
                    last_opened_crawl_id = table.Column<Guid>(type: "uuid", nullable: false),
                    history = table.Column<string>(type: "jsonb", nullable: true),
                    occurrences = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_findings", x => x.id);
                    table.ForeignKey(
                        name: "fk_findings_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_rule_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    severity_override = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    page_type_severities = table.Column<string>(type: "jsonb", nullable: false),
                    thresholds = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_rule_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_site_rule_settings_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_findings_site_id_fingerprint",
                table: "findings",
                columns: new[] { "site_id", "fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_findings_site_id_status",
                table: "findings",
                columns: new[] { "site_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_site_rule_settings_site_id_rule_code",
                table: "site_rule_settings",
                columns: new[] { "site_id", "rule_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "findings");

            migrationBuilder.DropTable(
                name: "site_rule_settings");

            migrationBuilder.DropColumn(
                name: "in_sitemap",
                table: "page_snapshots");

            migrationBuilder.DropColumn(
                name: "asset_checks",
                table: "crawls");

            migrationBuilder.DropColumn(
                name: "evaluated_at",
                table: "crawls");

            migrationBuilder.DropColumn(
                name: "new_findings",
                table: "crawls");

            migrationBuilder.DropColumn(
                name: "reopened_findings",
                table: "crawls");

            migrationBuilder.DropColumn(
                name: "resolved_findings",
                table: "crawls");
        }
    }
}
