using Microsoft.EntityFrameworkCore;
using PetCare.Domain.Entities;

namespace PetCare.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(PetCareDbContext context)
    {
        try
        {
            await context.Database.EnsureCreatedAsync();

            if (await context.Roles.AnyAsync())
            {
                return;
            }

            var roles = new List<Role>
            {
                new Role { RoleName = "admin", Description = "Quan tri vien he thong" },
                new Role { RoleName = "service_provider", Description = "Nha cung cap dich vu" },
                new Role { RoleName = "product_provider", Description = "Nha cung cap san pham" },
                new Role { RoleName = "staff", Description = "Nhan vien cham soc/grooming" },
                new Role { RoleName = "user", Description = "Nguoi dung thong thuong" }
            };
            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();

            var services = new List<Service>
            {
                new Service { ServiceName = "Tam va cat tia co ban", Description = "Dich vu tam, say va cat tia long co ban", DurationMinutes = 90, Price = 150000, IsActive = true },
                new Service { ServiceName = "Tam va cat tia cao cap", Description = "Dich vu tam, spa va cat tia long chuyen nghiep", DurationMinutes = 120, Price = 300000, IsActive = true },
                new Service { ServiceName = "Spa thu gian", Description = "Massage, cham soc da long cao cap", DurationMinutes = 60, Price = 250000, IsActive = true },
                new Service { ServiceName = "Luu tru thu cung", Description = "Dich vu luu tru theo ngay", DurationMinutes = 1440, Price = 200000, IsActive = true },
                new Service { ServiceName = "Tu van cham soc", Description = "Tu van cham soc thu cung", DurationMinutes = 30, Price = 0, IsActive = true }
            };
            await context.Services.AddRangeAsync(services);
            await context.SaveChangesAsync();

            var productCategories = new List<ProductCategory>
            {
                new ProductCategory { CategoryName = "Thuc an", Description = "Thuc an cho thu cung", DisplayOrder = 1, IsActive = true },
                new ProductCategory { CategoryName = "Phu kien", Description = "Phu kien cham soc thu cung", DisplayOrder = 2, IsActive = true },
                new ProductCategory { CategoryName = "Do choi", Description = "Do choi cho thu cung", DisplayOrder = 3, IsActive = true },
                new ProductCategory { CategoryName = "Thuoc & Vitamin", Description = "Thuoc va vitamin bo sung", DisplayOrder = 4, IsActive = true },
                new ProductCategory { CategoryName = "Ve sinh", Description = "San pham ve sinh", DisplayOrder = 5, IsActive = true },
                new ProductCategory { CategoryName = "Quan ao", Description = "Quan ao cho thu cung", DisplayOrder = 6, IsActive = true }
            };
            await context.ProductCategories.AddRangeAsync(productCategories);
            await context.SaveChangesAsync();

        }
        catch
        {
            throw;
        }
    }
}
