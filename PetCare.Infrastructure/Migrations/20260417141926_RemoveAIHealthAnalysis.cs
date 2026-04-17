using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAIHealthAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_health_analyses",
                schema: "petcare");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_health_analyses",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ai_model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ai_response = table.Column<string>(type: "text", nullable: false),
                    analysis_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    confidence_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    input_data = table.Column<string>(type: "text", nullable: false),
                    is_reviewed = table.Column<bool>(type: "boolean", nullable: false),
                    pet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recommendations = table.Column<string>(type: "text", nullable: true),
                    review_notes = table.Column<string>(type: "text", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tokens_used = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_health_analyses", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_health_analyses_users_reviewed_by",
                        column: x => x.reviewed_by,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_health_analyses_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_health_analyses_pet_id",
                schema: "petcare",
                table: "ai_health_analyses",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_health_analyses_pet_id_analysis_type",
                schema: "petcare",
                table: "ai_health_analyses",
                columns: new[] { "pet_id", "analysis_type" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_health_analyses_reviewed_by",
                schema: "petcare",
                table: "ai_health_analyses",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_ai_health_analyses_user_id",
                schema: "petcare",
                table: "ai_health_analyses",
                column: "user_id");
        }
    }
}
