using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketingOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScoresAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_channel_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protected_teams_webhook = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    protected_slack_webhook = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_channel_config", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_channel_config_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    finding_id = table.Column<Guid>(type: "uuid", nullable: true),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_deliveries_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    quiet_start = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    quiet_end = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    repeat_interval_hours = table.Column<int>(type: "integer", nullable: false),
                    score_drop_points = table.Column<int>(type: "integer", nullable: true),
                    channels = table.Column<string>(type: "jsonb", nullable: false),
                    recipients = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_rules_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    send_at = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    last_sent_on = table.Column<DateOnly>(type: "date", nullable: true),
                    recipients = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_schedules_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "score_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    taken_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_score_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_score_snapshots_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    href = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_notifications_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_findings_site_id_status_category",
                table: "findings",
                columns: new[] { "site_id", "status", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_channel_config_site_id",
                table: "notification_channel_config",
                column: "site_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_site_id_event_type_finding_id_sent_",
                table: "notification_deliveries",
                columns: new[] { "site_id", "event_type", "finding_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_rules_site_id_event_type",
                table: "notification_rules",
                columns: new[] { "site_id", "event_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_schedules_site_id_period",
                table: "report_schedules",
                columns: new[] { "site_id", "period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_score_snapshots_site_id_category_date",
                table: "score_snapshots",
                columns: new[] { "site_id", "category", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_notifications_site_id_read_at",
                table: "user_notifications",
                columns: new[] { "site_id", "read_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_channel_config");

            migrationBuilder.DropTable(
                name: "notification_deliveries");

            migrationBuilder.DropTable(
                name: "notification_rules");

            migrationBuilder.DropTable(
                name: "report_schedules");

            migrationBuilder.DropTable(
                name: "score_snapshots");

            migrationBuilder.DropTable(
                name: "user_notifications");

            migrationBuilder.DropIndex(
                name: "ix_findings_site_id_status_category",
                table: "findings");
        }
    }
}
