using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketingOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenderSamples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "crawl_render_enabled",
                table: "sites",
                type: "boolean",
                nullable: false,
                // Mevcut siteler sıkılık gereği render açık başlar (CrawlSettings varsayılanı).
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "crawl_render_samples_per_page_type",
                table: "sites",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.CreateTable(
                name: "render_samples",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    crawl_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    page_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    device = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    measured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    measurements = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_render_samples", x => x.id);
                    table.ForeignKey(
                        name: "fk_render_samples_crawls_crawl_id",
                        column: x => x.crawl_id,
                        principalTable: "crawls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_render_samples_crawl_id_page_type_id",
                table: "render_samples",
                columns: new[] { "crawl_id", "page_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_render_samples_crawl_id_url_device",
                table: "render_samples",
                columns: new[] { "crawl_id", "url", "device" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "render_samples");

            migrationBuilder.DropColumn(
                name: "crawl_render_enabled",
                table: "sites");

            migrationBuilder.DropColumn(
                name: "crawl_render_samples_per_page_type",
                table: "sites");
        }
    }
}
