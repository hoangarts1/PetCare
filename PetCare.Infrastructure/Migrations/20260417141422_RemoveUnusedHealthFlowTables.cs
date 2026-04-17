using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedHealthFlowTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_branches_branch_id",
                schema: "petcare",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_services_service_categories_category_id",
                schema: "petcare",
                table: "services");

            migrationBuilder.DropTable(
                name: "appointment_service_items",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "chat_messages",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "faq_items",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "health_records",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "health_reminders",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "product_reviews",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "service_categories",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "service_reviews",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "staff_schedules",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "user_subscriptions",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "chat_sessions",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "branches",
                schema: "petcare");

            migrationBuilder.DropTable(
                name: "subscription_packages",
                schema: "petcare");

            migrationBuilder.DropIndex(
                name: "IX_services_category_id",
                schema: "petcare",
                table: "services");

            migrationBuilder.DropIndex(
                name: "IX_appointments_branch_id",
                schema: "petcare",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "category_id",
                schema: "petcare",
                table: "services");

            migrationBuilder.DropColumn(
                name: "branch_id",
                schema: "petcare",
                table: "appointments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                schema: "petcare",
                table: "services",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                schema: "petcare",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "appointment_service_items",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_service_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_appointment_service_items_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalSchema: "petcare",
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_appointment_service_items_services_service_id",
                        column: x => x.service_id,
                        principalSchema: "petcare",
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    branch_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    opening_hours = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chat_sessions",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    session_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    session_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_chat_sessions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "faq_items",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    keywords = table.Column<string[]>(type: "text[]", nullable: true),
                    question = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    usage_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faq_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "health_records",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    diagnosis = table.Column<string>(type: "text", nullable: true),
                    heart_rate = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    pet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    temperature = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    treatment = table.Column<string>(type: "text", nullable: true),
                    weight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_health_records_users_recorded_by",
                        column: x => x.recorded_by,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "health_reminders",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    pet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reminder_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reminder_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    reminder_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_reminders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_reviews",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    images = table.Column<string[]>(type: "text[]", nullable: true),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    is_verified_purchase = table.Column<bool>(type: "boolean", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    review_text = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_reviews_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "petcare",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_reviews_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "petcare",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_reviews_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_categories",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    icon_url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_reviews",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: true),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    review_text = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_reviews_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalSchema: "petcare",
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_service_reviews_services_service_id",
                        column: x => x.service_id,
                        principalSchema: "petcare",
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_reviews_users_staff_id",
                        column: x => x.staff_id,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_reviews_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_packages",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    billing_cycle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    features = table.Column<string>(type: "text", nullable: true),
                    has_ai_health_tracking = table.Column<bool>(type: "boolean", nullable: false),
                    has_ai_recommendations = table.Column<bool>(type: "boolean", nullable: false),
                    has_early_disease_detection = table.Column<bool>(type: "boolean", nullable: false),
                    has_health_reminders = table.Column<bool>(type: "boolean", nullable: false),
                    has_nutritional_analysis = table.Column<bool>(type: "boolean", nullable: false),
                    has_priority_support = table.Column<bool>(type: "boolean", nullable: false),
                    has_vaccination_tracking = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    max_pets = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_packages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staff_schedules",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    work_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_staff_schedules_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "petcare",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_schedules_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    message_metadata = table.Column<string>(type: "text", nullable: true),
                    message_text = table.Column<string>(type: "text", nullable: false),
                    sender_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "petcare",
                        principalTable: "chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_subscriptions",
                schema: "petcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    next_billing_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_subscriptions_subscription_packages_subscription_packa~",
                        column: x => x.subscription_package_id,
                        principalSchema: "petcare",
                        principalTable: "subscription_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "petcare",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_services_category_id",
                schema: "petcare",
                table: "services",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_branch_id",
                schema: "petcare",
                table: "appointments",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointment_service_items_appointment_id",
                schema: "petcare",
                table: "appointment_service_items",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointment_service_items_service_id",
                schema: "petcare",
                table: "appointment_service_items",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_session_id",
                schema: "petcare",
                table: "chat_messages",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_sessions_user_id",
                schema: "petcare",
                table: "chat_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_health_records_pet_id",
                schema: "petcare",
                table: "health_records",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "IX_health_records_recorded_by",
                schema: "petcare",
                table: "health_records",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "IX_health_reminders_pet_id",
                schema: "petcare",
                table: "health_reminders",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_reviews_order_id",
                schema: "petcare",
                table: "product_reviews",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_reviews_product_id",
                schema: "petcare",
                table: "product_reviews",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_reviews_user_id",
                schema: "petcare",
                table: "product_reviews",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_reviews_appointment_id",
                schema: "petcare",
                table: "service_reviews",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_reviews_service_id",
                schema: "petcare",
                table: "service_reviews",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_reviews_staff_id",
                schema: "petcare",
                table: "service_reviews",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_reviews_user_id",
                schema: "petcare",
                table: "service_reviews",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_schedules_branch_id",
                schema: "petcare",
                table: "staff_schedules",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_schedules_user_id",
                schema: "petcare",
                table: "staff_schedules",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_subscription_package_id",
                schema: "petcare",
                table: "user_subscriptions",
                column: "subscription_package_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_user_id",
                schema: "petcare",
                table: "user_subscriptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_user_id_is_active",
                schema: "petcare",
                table: "user_subscriptions",
                columns: new[] { "user_id", "is_active" });

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_branches_branch_id",
                schema: "petcare",
                table: "appointments",
                column: "branch_id",
                principalSchema: "petcare",
                principalTable: "branches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_services_service_categories_category_id",
                schema: "petcare",
                table: "services",
                column: "category_id",
                principalSchema: "petcare",
                principalTable: "service_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
