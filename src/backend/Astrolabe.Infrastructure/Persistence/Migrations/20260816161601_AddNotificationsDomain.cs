using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astrolabe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_preferences",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    family = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    muted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_preferences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    body = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    route = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_preferences_member_id_family",
                schema: "astrolabe",
                table: "notification_preferences",
                columns: new[] { "member_id", "family" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_member_id",
                schema: "astrolabe",
                table: "notifications",
                column: "member_id",
                filter: "read_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_member_id_occurred_at",
                schema: "astrolabe",
                table: "notifications",
                columns: new[] { "member_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_preferences",
                schema: "astrolabe");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "astrolabe");
        }
    }
}
