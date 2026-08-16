using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astrolabe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationsDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reservations",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_copy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    library_id = table.Column<Guid>(type: "uuid", nullable: false),
                    borrowed_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    due_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    delivery = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    return_method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    handed_over_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    checked_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    days_late_at_check_in = table.Column<int>(type: "integer", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    delivery_fee_cents = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservations", x => x.id);
                    table.ForeignKey(
                        name: "fk_reservations_book_copies_book_copy_id",
                        column: x => x.book_copy_id,
                        principalSchema: "astrolabe",
                        principalTable: "book_copies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reservations_books_book_id",
                        column: x => x.book_id,
                        principalSchema: "astrolabe",
                        principalTable: "books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reservations_libraries_library_id",
                        column: x => x.library_id,
                        principalSchema: "astrolabe",
                        principalTable: "libraries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reservations_users_member_id",
                        column: x => x.member_id,
                        principalSchema: "astrolabe",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reservations_book_copy_id",
                schema: "astrolabe",
                table: "reservations",
                column: "book_copy_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_book_id",
                schema: "astrolabe",
                table: "reservations",
                column: "book_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_due_on",
                schema: "astrolabe",
                table: "reservations",
                column: "due_on");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_library_id_status",
                schema: "astrolabe",
                table: "reservations",
                columns: new[] { "library_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_reservations_member_id_idempotency_key",
                schema: "astrolabe",
                table: "reservations",
                columns: new[] { "member_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_member_id_status",
                schema: "astrolabe",
                table: "reservations",
                columns: new[] { "member_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reservations",
                schema: "astrolabe");
        }
    }
}
