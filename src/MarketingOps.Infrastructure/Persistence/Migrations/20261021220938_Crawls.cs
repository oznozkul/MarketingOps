using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketingOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Crawls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crawls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    triggered_by = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    queued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    pages_discovered = table.Column<int>(type: "integer", nullable: false),
                    pages_crawled = table.Column<int>(type: "integer", nullable: false),
                    pages_failed = table.Column<int>(type: "integer", nullable: false),
                    skipped_count = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    skipped_urls = table.Column<string>(type: "jsonb", nullable: true),
                    stages = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_crawls", x => x.id);
                    table.ForeignKey(
                        name: "fk_crawls_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    crawl_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    normalized_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    page_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    redirect_chain = table.Column<string>(type: "jsonb", nullable: false),
                    response_time_ms = table.Column<int>(type: "integer", nullable: true),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    content_encoding = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    cache_control = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    html_size_bytes = table.Column<int>(type: "integer", nullable: true),
                    fetch_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fetched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_page_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_page_snapshots_crawls_crawl_id",
                        column: x => x.crawl_id,
                        principalTable: "crawls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_crawls_one_active_per_site",
                table: "crawls",
                column: "site_id",
                unique: true,
                filter: "status IN ('Queued', 'Running')");

            migrationBuilder.CreateIndex(
                name: "ix_crawls_site_id_number",
                table: "crawls",
                columns: new[] { "site_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_page_snapshots_crawl_id_normalized_url",
                table: "page_snapshots",
                columns: new[] { "crawl_id", "normalized_url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_page_snapshots_crawl_id_page_type_id",
                table: "page_snapshots",
                columns: new[] { "crawl_id", "page_type_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "page_snapshots");

            migrationBuilder.DropTable(
                name: "crawls");
        }
    }
}
