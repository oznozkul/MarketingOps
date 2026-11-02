using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketingOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SiteSetupStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "setup_step",
                table: "sites",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "setup_step",
                table: "sites");
        }
    }
}
