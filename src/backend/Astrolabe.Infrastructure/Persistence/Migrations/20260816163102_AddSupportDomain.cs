using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astrolabe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tickets",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    library_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    agent_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agent_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    review = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tickets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ticket_messages",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    author_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    written_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_ticket_messages_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalSchema: "astrolabe",
                        principalTable: "tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ticket_messages_ticket_id",
                schema: "astrolabe",
                table: "ticket_messages",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_library_id_status",
                schema: "astrolabe",
                table: "tickets",
                columns: new[] { "library_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_tickets_member_id_updated_at",
                schema: "astrolabe",
                table: "tickets",
                columns: new[] { "member_id", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tickets_reference",
                schema: "astrolabe",
                table: "tickets",
                column: "reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_messages",
                schema: "astrolabe");

            migrationBuilder.DropTable(
                name: "tickets",
                schema: "astrolabe");
        }
    }
}
