using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketingOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSitesAndPageTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    domain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    is_ownership_verified = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    crawl_schedule = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    crawl_max_requests_per_second = table.Column<int>(type: "integer", nullable: false),
                    crawl_max_pages = table.Column<int>(type: "integer", nullable: false),
                    crawl_excluded_path_patterns = table.Column<string[]>(type: "text[]", nullable: false),
                    expected_ga4_measurement_id = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    expected_gtm_container_id = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    expected_meta_pixel_id = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    expected_google_ads_conversion_id = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sites", x => x.id);
                    table.ForeignKey(
                        name: "fk_sites_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "page_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    conditions = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_page_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_page_types_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_page_types_site_id",
                table: "page_types",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "ix_sites_organization_id_domain",
                table: "sites",
                columns: new[] { "organization_id", "domain" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "page_types");

            migrationBuilder.DropTable(
                name: "sites");

            migrationBuilder.DropTable(
                name: "organizations");
        }
    }
}
