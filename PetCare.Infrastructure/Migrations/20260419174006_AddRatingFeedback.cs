using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rating_feedbacks",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rating_feedbacks", x => x.id);
                    table.ForeignKey(
                        name: "FK_rating_feedbacks_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalSchema: "petcare",
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rating_feedbacks_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rating_feedbacks_appointment_id",
                schema: "petcare",
                table: "rating_feedbacks",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rating_feedbacks_user_id",
                schema: "petcare",
                table: "rating_feedbacks",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rating_feedbacks",
                schema: "petcare");
        }
    }
}
