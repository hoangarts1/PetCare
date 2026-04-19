using Microsoft.EntityFrameworkCore;
using PetCare.Domain.Common;
using PetCare.Domain.Entities;

namespace PetCare.Infrastructure.Data;

public class PetCareDbContext : DbContext
{
    public PetCareDbContext(DbContextOptions<PetCareDbContext> options) : base(options)
    {
    }

    // User Management
    public DbSet<Role> Roles { get; set; }
    public DbSet<User> Users { get; set; }

    // E-Commerce
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    // Services & Appointments
    public DbSet<Service> Services { get; set; }
    public DbSet<StaffService> StaffServices { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<AppointmentStatusHistory> AppointmentStatusHistories { get; set; }
    public DbSet<RatingFeedback> RatingFeedbacks { get; set; }

    // Notifications
    public DbSet<Notification> Notifications { get; set; }

    // Payments
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("petcare");

        ConfigureUserEntities(modelBuilder);
        ConfigureProductEntities(modelBuilder);
        ConfigureServiceEntities(modelBuilder);
        ConfigureNotificationEntities(modelBuilder);
        ConfigurePaymentEntities(modelBuilder);
    }

    private void ConfigureUserEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(e => e.FullName).HasColumnName("full_name").IsRequired().HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.District).HasColumnName("district").HasMaxLength(100);
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleName).HasColumnName("role_name").IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.RoleName).IsUnique();
        });
    }

    private void ConfigureProductEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.ToTable("product_categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryName).HasColumnName("category_name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.ParentCategoryId).HasColumnName("parent_category_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(e => e.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");

            entity.Property(e => e.ProductName).HasColumnName("product_name").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Price).HasColumnName("price").HasPrecision(12, 2);
            entity.Property(e => e.SalePrice).HasColumnName("sale_price").HasPrecision(12, 2);
            entity.Property(e => e.StockQuantity).HasColumnName("stock_quantity");
            entity.Property(e => e.Sku).HasColumnName("sku").HasMaxLength(100);
            entity.Property(e => e.Weight).HasColumnName("weight").HasPrecision(8, 2);
            entity.Property(e => e.Dimensions).HasColumnName("dimensions").HasMaxLength(50);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Sku).IsUnique();
            entity.HasIndex(e => e.CategoryId);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("product_images");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url").IsRequired();
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProductId);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("cart_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.CartItems)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.OrderNumber).HasColumnName("order_number").IsRequired().HasMaxLength(50);
            entity.Property(e => e.OrderStatus).HasColumnName("order_status").IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount").HasPrecision(12, 2);
            entity.Property(e => e.ShippingFee).HasColumnName("shipping_fee").HasPrecision(10, 2);
            entity.Property(e => e.DiscountAmount).HasColumnName("discount_amount").HasPrecision(10, 2);
            entity.Property(e => e.FinalAmount).HasColumnName("final_amount").HasPrecision(12, 2);
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status").HasMaxLength(50);
            entity.Property(e => e.ShippingAddress).HasColumnName("shipping_address").IsRequired();
            entity.Property(e => e.ShippingPhone).HasColumnName("shipping_phone").IsRequired().HasMaxLength(20);
            entity.Property(e => e.ShippingName).HasColumnName("shipping_name").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OrderedAt).HasColumnName("ordered_at");

            entity.Ignore(e => e.CreatedAt);
            entity.Ignore(e => e.UpdatedAt);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductName).HasColumnName("product_name").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2);
            entity.Property(e => e.TotalPrice).HasColumnName("total_price").HasPrecision(12, 2);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.OrderId);
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("order_status_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(50);
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.StatusHistory)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.OrderId);
        });
    }

    private void ConfigureServiceEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable("services");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ServiceName).HasColumnName("service_name").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.Price).HasColumnName("price").HasPrecision(10, 2);
            entity.Property(e => e.IsHomeService).HasColumnName("is_home_service");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<StaffService>(entity =>
        {
            entity.ToTable("staff_services");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Service)
                .WithMany(s => s.StaffServices)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.ServiceId }).IsUnique();
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("appointments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Pet).HasColumnName("pet").HasMaxLength(100);
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.AppointmentType).HasColumnName("appointment_type").IsRequired().HasMaxLength(50);
            entity.Property(e => e.AppointmentStatus).HasColumnName("appointment_status").HasMaxLength(50);
            entity.Property(e => e.AssignedStaffId).HasColumnName("assigned_staff_id");
            entity.Property(e => e.AppointmentDate).HasColumnName("appointment_date");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.ServiceAddress).HasColumnName("service_address");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CancellationReason).HasColumnName("cancellation_reason");
            entity.Property(e => e.CheckInCode).HasColumnName("check_in_code").HasMaxLength(5);
            entity.Property(e => e.CheckedInAt).HasColumnName("checked_in_at");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.BillNumber).HasColumnName("bill_number").HasMaxLength(40);
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount").HasPrecision(10, 2);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Appointments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Service)
                .WithMany(s => s.Appointments)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssignedStaff)
                .WithMany()
                .HasForeignKey(e => e.AssignedStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.AppointmentDate);
            entity.HasIndex(e => e.CheckInCode);
        });

        modelBuilder.Entity<AppointmentStatusHistory>(entity =>
        {
            entity.ToTable("appointment_status_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(50);
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Appointment)
                .WithMany(a => a.StatusHistory)
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.AppointmentId);
        });

        modelBuilder.Entity<RatingFeedback>(entity =>
        {
            entity.ToTable("rating_feedbacks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Appointment)
                .WithMany(a => a.RatingFeedbacks)
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.RatingFeedbacks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.AppointmentId).IsUnique();
            entity.HasIndex(e => e.UserId);
        });
    }

    private void ConfigureNotificationEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.NotificationType).HasColumnName("notification_type").IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).HasColumnName("title").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Message).HasColumnName("message").IsRequired();
            entity.Property(e => e.LinkUrl).HasColumnName("link_url");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsRead }).HasFilter("is_read = false");
        });
    }

    private void ConfigurePaymentEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").IsRequired().HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status").IsRequired().HasMaxLength(20);
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(10, 2);
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id").HasMaxLength(255);
            entity.Property(e => e.PaymentGatewayResponse).HasColumnName("payment_gateway_response");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.RefundReason).HasColumnName("refund_reason");
            entity.Property(e => e.RefundedAt).HasColumnName("refunded_at");
            entity.Property(e => e.RefundAmount).HasColumnName("refund_amount").HasPrecision(10, 2);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TransactionId);
        });
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is AuditableEntity && e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            ((AuditableEntity)entry.Entity).UpdatedAt = DateTime.UtcNow;
        }
    }
}
