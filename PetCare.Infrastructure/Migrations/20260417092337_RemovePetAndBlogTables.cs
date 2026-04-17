using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePetAndBlogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_health_analyses_pets_pet_id",
                schema: "petcare",
                table: "ai_health_analyses");

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_pets_pet_id",
                schema: "petcare",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_health_records_pets_pet_id",
                schema: "petcare",
                table: "health_records");

            migrationBuilder.DropForeignKey(
                name: "FK_health_reminders_pets_pet_id",
                schema: "petcare",
                table: "health_reminders");

            migrationBuilder.DropTable(
                name: "blog_comments",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "blog_likes",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "blog_post_tags",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "pets",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "blog_posts",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "tags",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "pet_breeds",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "blog_categories",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "pet_species",
                schema: "petcare");

            migrationBuilder.DropIndex(
                name: "IX_appointments_pet_id",
                schema: "petcare",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "pet_id",
                schema: "petcare",
                table: "appointments");

            migrationBuilder.AddColumn<string>(
                name: "pet",
                schema: "petcare",
                table: "appointments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pet",
                schema: "petcare",
                table: "appointments");

            migrationBuilder.AddColumn<Guid>(
                name: "pet_id",
                schema: "petcare",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "blog_categories",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blog_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pet_species",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    species_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_species", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tag_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "blog_posts",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    excerpt = table.Column<string>(type: "text", nullable: true),
                    featured_image_url = table.Column<string>(type: "text", nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    view_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blog_posts", x => x.id);
                    table.ForeignKey(
                        name: "FK_blog_posts_blog_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "petcare",
                        principalTable: "blog_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_blog_posts_users_author_id",
                        column: x => x.author_id,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pet_breeds",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    species_id = table.Column<Guid>(type: "uuid", nullable: false),
                    breed_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    characteristics = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_breeds", x => x.id);
                    table.ForeignKey(
                        name: "FK_pet_breeds_pet_species_species_id",
                        column: x => x.species_id,
                        principalSchema: "petcare",
                        principalTable: "pet_species",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "blog_comments",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    comment_text = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blog_comments", x => x.id);
                    table.ForeignKey(
                        name: "FK_blog_comments_blog_comments_parent_comment_id",
                        column: x => x.parent_comment_id,
                        principalSchema: "petcare",
                        principalTable: "blog_comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_blog_comments_blog_posts_post_id",
                        column: x => x.post_id,
                        principalSchema: "petcare",
                        principalTable: "blog_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_blog_comments_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "blog_likes",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blog_likes", x => x.id);
                    table.ForeignKey(
                        name: "FK_blog_likes_blog_posts_post_id",
                        column: x => x.post_id,
                        principalSchema: "petcare",
                        principalTable: "blog_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_blog_likes_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "blog_post_tags",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blog_post_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_blog_post_tags_blog_posts_post_id",
                        column: x => x.post_id,
                        principalSchema: "petcare",
                        principalTable: "blog_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_blog_post_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "petcare",
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pets",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    breed_id = table.Column<Guid>(type: "uuid", nullable: true),
                    species_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    color = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    microchip_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    pet_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    special_notes = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    weight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pets", x => x.id);
                    table.ForeignKey(
                        name: "FK_pets_pet_breeds_breed_id",
                        column: x => x.breed_id,
                        principalSchema: "petcare",
                        principalTable: "pet_breeds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pets_pet_species_species_id",
                        column: x => x.species_id,
                        principalSchema: "petcare",
                        principalTable: "pet_species",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pets_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_pet_id",
                schema: "petcare",
                table: "appointments",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "IX_blog_categories_slug",
                schema: "petcare",
                table: "blog_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_blog_comments_parent_comment_id",
                schema: "petcare",
                table: "blog_comments",
                column: "parent_comment_id");

            migrationBuilder.CreateIndex(
                name: "IX_blog_comments_post_id",
                schema: "petcare",
                table: "blog_comments",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "IX_blog_comments_user_id",
                schema: "petcare",
                table: "blog_comments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_blog_likes_post_id_user_id",
                schema: "petcare",
                table: "blog_likes",
                columns: new[] { "post_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_blog_likes_user_id",
                schema: "petcare",
                table: "blog_likes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_blog_post_tags_post_id_tag_id",
                schema: "petcare",
                table: "blog_post_tags",
                columns: new[] { "post_id", "tag_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_blog_post_tags_tag_id",
                schema: "petcare",
                table: "blog_post_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_author_id",
                schema: "petcare",
                table: "blog_posts",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_category_id",
                schema: "petcare",
                table: "blog_posts",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_slug",
                schema: "petcare",
                table: "blog_posts",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_status",
                schema: "petcare",
                table: "blog_posts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_pet_breeds_species_id",
                schema: "petcare",
                table: "pet_breeds",
                column: "species_id");

            migrationBuilder.CreateIndex(
                name: "IX_pets_breed_id",
                schema: "petcare",
                table: "pets",
                column: "breed_id");

            migrationBuilder.CreateIndex(
                name: "IX_pets_species_id",
                schema: "petcare",
                table: "pets",
                column: "species_id");

            migrationBuilder.CreateIndex(
                name: "IX_pets_user_id",
                schema: "petcare",
                table: "pets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tags_slug",
                schema: "petcare",
                table: "tags",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tags_tag_name",
                schema: "petcare",
                table: "tags",
                column: "tag_name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ai_health_analyses_pets_pet_id",
                schema: "petcare",
                table: "ai_health_analyses",
                column: "pet_id",
                principalSchema: "petcare",
                principalTable: "pets",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_pets_pet_id",
                schema: "petcare",
                table: "appointments",
                column: "pet_id",
                principalSchema: "petcare",
                principalTable: "pets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_health_records_pets_pet_id",
                schema: "petcare",
                table: "health_records",
                column: "pet_id",
                principalSchema: "petcare",
                principalTable: "pets",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_health_reminders_pets_pet_id",
                schema: "petcare",
                table: "health_reminders",
                column: "pet_id",
                principalSchema: "petcare",
                principalTable: "pets",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
