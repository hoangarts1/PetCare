using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedColumnsForSupabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_services_service_id",
                schema: "petcare",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_products_users_ProviderId",
                schema: "petcare",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_ProviderId",
                schema: "petcare",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_sku",
                schema: "petcare",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_appointments_service_id",
                schema: "petcare",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                schema: "petcare",
                table: "products");

            migrationBuilder.DropColumn(
                name: "sale_price",
                schema: "petcare",
                table: "products");

            migrationBuilder.DropColumn(
                name: "sku",
                schema: "petcare",
                table: "products");

            migrationBuilder.DropColumn(
                name: "weight",
                schema: "petcare",
                table: "products");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                schema: "petcare",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_fee",
                schema: "petcare",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "end_time",
                schema: "petcare",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "service_id",
                schema: "petcare",
                table: "appointments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                schema: "petcare",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sale_price",
                schema: "petcare",
                table: "products",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sku",
                schema: "petcare",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "weight",
                schema: "petcare",
                table: "products",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                schema: "petcare",
                table: "orders",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_fee",
                schema: "petcare",
                table: "orders",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "end_time",
                schema: "petcare",
                table: "appointments",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<Guid>(
                name: "service_id",
                schema: "petcare",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_ProviderId",
                schema: "petcare",
                table: "products",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_products_sku",
                schema: "petcare",
                table: "products",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_appointments_service_id",
                schema: "petcare",
                table: "appointments",
                column: "service_id");

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_services_service_id",
                schema: "petcare",
                table: "appointments",
                column: "service_id",
                principalSchema: "petcare",
                principalTable: "services",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_products_users_ProviderId",
                schema: "petcare",
                table: "products",
                column: "ProviderId",
                principalSchema: "petcare",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
