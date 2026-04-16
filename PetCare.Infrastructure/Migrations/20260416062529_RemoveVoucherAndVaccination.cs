using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVoucherAndVaccination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_vouchers_VoucherId",
                schema: "petcare",
                table: "orders");

            migrationBuilder.DropTable(
                name: "vaccinations",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "vaccine_catalog",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "voucher_usages",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "vouchers",
                schema: "petcare");

            migrationBuilder.DropIndex(
                name: "IX_orders_VoucherId",
                schema: "petcare",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "VoucherCode",
                schema: "petcare",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "VoucherId",
                schema: "petcare",
                table: "orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoucherCode",
                schema: "petcare",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VoucherId",
                schema: "petcare",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "vaccinations",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    administered_by = table.Column<Guid>(type: "uuid", nullable: true),
                    batch_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    pet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vaccine_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    vaccine_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    vaccination_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vaccinations", x => x.id);
                    table.ForeignKey(
                        name: "FK_vaccinations_pets_pet_id",
                        column: x => x.pet_id,
                        principalSchema: "petcare",
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vaccinations_users_administered_by",
                        column: x => x.administered_by,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vaccine_catalog",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aliases = table.Column<string>(type: "text", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    default_interval_days = table.Column<int>(type: "integer", nullable: true),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vaccine_catalog", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vouchers",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    applicable_product_categories = table.Column<string>(type: "text", nullable: true),
                    applicable_services = table.Column<string>(type: "text", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    discount_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    maximum_discount_amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    minimum_order_amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usage_limit = table.Column<int>(type: "integer", nullable: true),
                    used_count = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vouchers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "voucher_usages",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voucher_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voucher_usages", x => x.id);
                    table.ForeignKey(
                        name: "FK_voucher_usages_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "petcare",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voucher_usages_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voucher_usages_vouchers_voucher_id",
                        column: x => x.voucher_id,
                        principalSchema: "petcare",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "petcare",
                table: "vaccine_catalog",
                columns: new[] { "id", "aliases", "code", "created_at", "default_interval_days", "display_name", "is_active" },
                values: new object[,]
                {
                    { new Guid("9ef90f99-4b40-4e09-9f4b-1c39b44ea001"), "rabies;dai;d?i;tiem dai;tiêm d?i", "RABIES", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 365, "Rabies (Dai)", true },
                    { new Guid("9ef90f99-4b40-4e09-9f4b-1c39b44ea002"), "dhpp;dhlpp;5 in 1;5in1;7 in 1;7in1;distemper;parvo;care;ho cui", "DHPP", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 365, "DHPP Core Vaccine", true },
                    { new Guid("9ef90f99-4b40-4e09-9f4b-1c39b44ea003"), "bordetella;kennel cough;ho cun cho;ho cui chó", "BORDETELLA", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 365, "Bordetella (Kennel Cough)", true },
                    { new Guid("9ef90f99-4b40-4e09-9f4b-1c39b44ea004"), "lepto;leptospirosis", "LEPTO", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 365, "Leptospirosis", true },
                    { new Guid("9ef90f99-4b40-4e09-9f4b-1c39b44ea005"), "parvo;parvovirus", "PARVO_ONLY", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 365, "Parvovirus", true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_orders_VoucherId",
                schema: "petcare",
                table: "orders",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_vaccinations_administered_by",
                schema: "petcare",
                table: "vaccinations",
                column: "administered_by");

            migrationBuilder.CreateIndex(
                name: "IX_vaccinations_pet_id",
                schema: "petcare",
                table: "vaccinations",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "IX_vaccine_catalog_code",
                schema: "petcare",
                table: "vaccine_catalog",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_voucher_usages_order_id",
                schema: "petcare",
                table: "voucher_usages",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_usages_user_id",
                schema: "petcare",
                table: "voucher_usages",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_usages_voucher_id",
                schema: "petcare",
                table: "voucher_usages",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_vouchers_code",
                schema: "petcare",
                table: "vouchers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vouchers_is_active_valid_from_valid_to",
                schema: "petcare",
                table: "vouchers",
                columns: new[] { "is_active", "valid_from", "valid_to" });

            migrationBuilder.AddForeignKey(
                name: "FK_orders_vouchers_VoucherId",
                schema: "petcare",
                table: "orders",
                column: "VoucherId",
                principalSchema: "petcare",
                principalTable: "vouchers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
