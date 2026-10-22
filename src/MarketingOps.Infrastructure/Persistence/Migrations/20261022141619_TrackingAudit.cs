using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketingOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackingAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "journey_cart_url",
                table: "sites",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "journey_checkout_url",
                table: "sites",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "journey_consent_button_selector",
                table: "sites",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "journey_thank_you_url",
                table: "sites",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "order_source_protected_connection",
                table: "sites",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "order_source_query",
                table: "sites",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "order_comparison_daily",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    order_count = table.Column<int>(type: "integer", nullable: true),
                    order_revenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ga4_purchase_count = table.Column<long>(type: "bigint", nullable: true),
                    ga4_purchase_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    gap_percent = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_comparison_daily", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_comparison_daily_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tag_audit_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consent_tested = table.Column<bool>(type: "boolean", nullable: false),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    order_source_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tag_audit_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_tag_audit_runs_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tag_audit_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    page_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    page_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    vendor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    found_ids = table.Column<string>(type: "jsonb", nullable: false),
                    requests = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tag_audit_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_tag_audit_results_tag_audit_runs_audit_run_id",
                        column: x => x.audit_run_id,
                        principalTable: "tag_audit_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_comparison_daily_site_id_date",
                table: "order_comparison_daily",
                columns: new[] { "site_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tag_audit_results_audit_run_id_page_label_vendor",
                table: "tag_audit_results",
                columns: new[] { "audit_run_id", "page_label", "vendor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tag_audit_runs_site_id_number",
                table: "tag_audit_runs",
                columns: new[] { "site_id", "number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_comparison_daily");

            migrationBuilder.DropTable(
                name: "tag_audit_results");

            migrationBuilder.DropTable(
                name: "tag_audit_runs");

            migrationBuilder.DropColumn(
                name: "journey_cart_url",
                table: "sites");

            migrationBuilder.DropColumn(
                name: "journey_checkout_url",
                table: "sites");

            migrationBuilder.DropColumn(
                name: "journey_consent_button_selector",
                table: "sites");

            migrationBuilder.DropColumn(
                name: "journey_thank_you_url",
                table: "sites");

            migrationBuilder.DropColumn(
                name: "order_source_protected_connection",
                table: "sites");

            migrationBuilder.DropColumn(
                name: "order_source_query",
                table: "sites");
        }
    }
}
