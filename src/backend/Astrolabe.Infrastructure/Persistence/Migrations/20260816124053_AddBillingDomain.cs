using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astrolabe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "desk_payments",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    library_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    fine_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    amount_cents = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_desk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_desk_payments_libraries_library_id",
                        column: x => x.library_id,
                        principalSchema: "astrolabe",
                        principalTable: "libraries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_desk_payments_users_member_id",
                        column: x => x.member_id,
                        principalSchema: "astrolabe",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fines",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    library_id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    days_late = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    assessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    settled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    desk_payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_cents = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fines", x => x.id);
                    table.ForeignKey(
                        name: "fk_fines_libraries_library_id",
                        column: x => x.library_id,
                        principalSchema: "astrolabe",
                        principalTable: "libraries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fines_reservations_reservation_id",
                        column: x => x.reservation_id,
                        principalSchema: "astrolabe",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fines_users_member_id",
                        column: x => x.member_id,
                        principalSchema: "astrolabe",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ledger_entries",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    fine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    amount_cents = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_ledger_entries_users_member_id",
                        column: x => x.member_id,
                        principalSchema: "astrolabe",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    last4 = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                    expiry_month_year = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    cardholder_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_methods_users_member_id",
                        column: x => x.member_id,
                        principalSchema: "astrolabe",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_desk_payments_code",
                schema: "astrolabe",
                table: "desk_payments",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_desk_payments_library_id_status",
                schema: "astrolabe",
                table: "desk_payments",
                columns: new[] { "library_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_desk_payments_member_id_status",
                schema: "astrolabe",
                table: "desk_payments",
                columns: new[] { "member_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_fines_desk_payment_id",
                schema: "astrolabe",
                table: "fines",
                column: "desk_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_fines_library_id",
                schema: "astrolabe",
                table: "fines",
                column: "library_id");

            migrationBuilder.CreateIndex(
                name: "ix_fines_member_id_status",
                schema: "astrolabe",
                table: "fines",
                columns: new[] { "member_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_fines_reservation_id",
                schema: "astrolabe",
                table: "fines",
                column: "reservation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_member_id_occurred_at",
                schema: "astrolabe",
                table: "ledger_entries",
                columns: new[] { "member_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_member_id",
                schema: "astrolabe",
                table: "payment_methods",
                column: "member_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "desk_payments",
                schema: "astrolabe");

            migrationBuilder.DropTable(
                name: "fines",
                schema: "astrolabe");

            migrationBuilder.DropTable(
                name: "ledger_entries",
                schema: "astrolabe");

            migrationBuilder.DropTable(
                name: "payment_methods",
                schema: "astrolabe");
        }
    }
}
