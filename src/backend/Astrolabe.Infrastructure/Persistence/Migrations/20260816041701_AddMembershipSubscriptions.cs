using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astrolabe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscriptions",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    cycle_started_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cycle_renews_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cycle_anchor_day = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    scheduled_change_target = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    scheduled_change_effective_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    scheduled_change_requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    city_changes_this_cycle = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_cycle_renews_on",
                schema: "astrolabe",
                table: "subscriptions",
                column: "cycle_renews_on",
                filter: "ended_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_member_id",
                schema: "astrolabe",
                table: "subscriptions",
                column: "member_id",
                unique: true,
                filter: "ended_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "astrolabe");
        }
    }
}
