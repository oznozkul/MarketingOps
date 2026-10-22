using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketingOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GoogleConnectors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_tracker_connections");

            migrationBuilder.DropColumn(
                name: "external_tracker_key",
                table: "work_tasks");

            migrationBuilder.DropColumn(
                name: "external_url",
                table: "work_tasks");

            migrationBuilder.CreateTable(
                name: "connector_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    external_account_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    external_account_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    account_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    protected_secret = table.Column<string>(type: "text", nullable: true),
                    last_sync_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    scopes = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_connector_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_connector_accounts_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ga4_event_daily",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    event_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_count = table.Column<long>(type: "bigint", nullable: false),
                    event_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ga4_event_daily", x => x.id);
                    table.ForeignKey(
                        name: "fk_ga4_event_daily_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "search_performance_daily",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    clicks = table.Column<int>(type: "integer", nullable: false),
                    impressions = table.Column<int>(type: "integer", nullable: false),
                    ctr = table.Column<double>(type: "double precision", nullable: false),
                    position = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_search_performance_daily", x => x.id);
                    table.ForeignKey(
                        name: "fk_search_performance_daily_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sitemap_statuses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    last_submitted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_downloaded = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_pending = table.Column<bool>(type: "boolean", nullable: false),
                    errors = table.Column<int>(type: "integer", nullable: false),
                    warnings = table.Column<int>(type: "integer", nullable: false),
                    submitted = table.Column<long>(type: "bigint", nullable: true),
                    indexed = table.Column<long>(type: "bigint", nullable: true),
                    synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sitemap_statuses", x => x.id);
                    table.ForeignKey(
                        name: "fk_sitemap_statuses_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "url_index_statuses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    verdict = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    coverage_state = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    indexing_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_crawl_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    inspected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_url_index_statuses", x => x.id);
                    table.ForeignKey(
                        name: "fk_url_index_statuses_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "web_vitals_daily",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    page_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    strategy = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    lcp_ms = table.Column<int>(type: "integer", nullable: true),
                    inp_ms = table.Column<int>(type: "integer", nullable: true),
                    cls = table.Column<double>(type: "double precision", nullable: true),
                    ttfb_ms = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_web_vitals_daily", x => x.id);
                    table.ForeignKey(
                        name: "fk_web_vitals_daily_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_connector_accounts_site_id_type",
                table: "connector_accounts",
                columns: new[] { "site_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ga4_event_daily_site_id_date_event_name",
                table: "ga4_event_daily",
                columns: new[] { "site_id", "date", "event_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_search_performance_daily_site_id_date_url",
                table: "search_performance_daily",
                columns: new[] { "site_id", "date", "url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sitemap_statuses_site_id_path",
                table: "sitemap_statuses",
                columns: new[] { "site_id", "path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_url_index_statuses_site_id_url",
                table: "url_index_statuses",
                columns: new[] { "site_id", "url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_web_vitals_daily_site_id_date_url_strategy",
                table: "web_vitals_daily",
                columns: new[] { "site_id", "date", "url", "strategy" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "connector_accounts");

            migrationBuilder.DropTable(
                name: "ga4_event_daily");

            migrationBuilder.DropTable(
                name: "search_performance_daily");

            migrationBuilder.DropTable(
                name: "sitemap_statuses");

            migrationBuilder.DropTable(
                name: "url_index_statuses");

            migrationBuilder.DropTable(
                name: "web_vitals_daily");

            migrationBuilder.AddColumn<string>(
                name: "external_tracker_key",
                table: "work_tasks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_url",
                table: "work_tasks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "task_tracker_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    issue_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    project_key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    protected_api_token = table.Column<string>(type: "text", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "ix_task_tracker_connections_site_id",
                table: "task_tracker_connections",
                column: "site_id",
                unique: true);
        }
    }
}
