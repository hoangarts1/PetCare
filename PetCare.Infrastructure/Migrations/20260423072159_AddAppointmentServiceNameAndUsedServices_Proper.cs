using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentServiceNameAndUsedServices_Proper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "service_name",
                schema: "petcare",
                table: "appointments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "appointment_used_services",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_used_services", x => x.id);
                    table.ForeignKey(
                        name: "FK_appointment_used_services_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalSchema: "petcare",
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_appointment_used_services_services_service_id",
                        column: x => x.service_id,
                        principalSchema: "petcare",
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_service_name",
                schema: "petcare",
                table: "appointments",
                column: "service_name");

            migrationBuilder.CreateIndex(
                name: "IX_appointment_used_services_appointment_id",
                schema: "petcare",
                table: "appointment_used_services",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointment_used_services_service_id",
                schema: "petcare",
                table: "appointment_used_services",
                column: "service_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointment_used_services",
                schema: "petcare");

            migrationBuilder.DropIndex(
                name: "IX_appointments_service_name",
                schema: "petcare",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "service_name",
                schema: "petcare",
                table: "appointments");
        }
    }
}

