using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceIdToAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "service_id",
                schema: "petcare",
                table: "appointments",
                type: "uuid",
                nullable: true);

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
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_services_service_id",
                schema: "petcare",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointments_service_id",
                schema: "petcare",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "service_id",
                schema: "petcare",
                table: "appointments");
        }
    }
}
