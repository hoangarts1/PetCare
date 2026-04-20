using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductCategoryParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_product_categories_product_categories_parent_category_id",
                schema: "petcare",
                table: "product_categories");

            migrationBuilder.DropIndex(
                name: "IX_product_categories_parent_category_id",
                schema: "petcare",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "parent_category_id",
                schema: "petcare",
                table: "product_categories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "parent_category_id",
                schema: "petcare",
                table: "product_categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_categories_parent_category_id",
                schema: "petcare",
                table: "product_categories",
                column: "parent_category_id");

            migrationBuilder.AddForeignKey(
                name: "FK_product_categories_product_categories_parent_category_id",
                schema: "petcare",
                table: "product_categories",
                column: "parent_category_id",
                principalSchema: "petcare",
                principalTable: "product_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
