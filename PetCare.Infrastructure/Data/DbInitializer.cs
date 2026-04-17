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

            var serviceCategories = new List<ServiceCategory>
            {
                new ServiceCategory { CategoryName = "Grooming", Description = "Dich vu cat tia, tam rua", IconUrl = "/icons/grooming.svg" },
                new ServiceCategory { CategoryName = "Spa & Cham soc", Description = "Dich vu spa va cham soc sac dep", IconUrl = "/icons/spa.svg" },
                new ServiceCategory { CategoryName = "Khach san thu cung", Description = "Dich vu luu tru thu cung", IconUrl = "/icons/hotel.svg" },
                new ServiceCategory { CategoryName = "Huong dan", Description = "Dich vu huong dan thu cung", IconUrl = "/icons/training.svg" },
                new ServiceCategory { CategoryName = "Tu van suc khoe", Description = "Tu van va gioi thieu dich vu thu y doi tac", IconUrl = "/icons/consultation.svg" }
            };
            await context.ServiceCategories.AddRangeAsync(serviceCategories);
            await context.SaveChangesAsync();

            var groomingCategory = serviceCategories.First(sc => sc.CategoryName == "Grooming");
            var spaCategory = serviceCategories.First(sc => sc.CategoryName == "Spa & Cham soc");
            var hotelCategory = serviceCategories.First(sc => sc.CategoryName == "Khach san thu cung");
            var consultationCategory = serviceCategories.First(sc => sc.CategoryName == "Tu van suc khoe");

            var services = new List<Service>
            {
                new Service { CategoryId = groomingCategory.Id, ServiceName = "Tam va cat tia co ban", Description = "Dich vu tam, say va cat tia long co ban", DurationMinutes = 90, Price = 150000, IsActive = true },
                new Service { CategoryId = groomingCategory.Id, ServiceName = "Tam va cat tia cao cap", Description = "Dich vu tam, spa va cat tia long chuyen nghiep", DurationMinutes = 120, Price = 300000, IsActive = true },
                new Service { CategoryId = spaCategory.Id, ServiceName = "Spa thu gian", Description = "Massage, cham soc da long cao cap", DurationMinutes = 60, Price = 250000, IsActive = true },
                new Service { CategoryId = hotelCategory.Id, ServiceName = "Luu tru thu cung", Description = "Dich vu luu tru theo ngay", DurationMinutes = 1440, Price = 200000, IsActive = true },
                new Service { CategoryId = consultationCategory.Id, ServiceName = "Tu van suc khoe", Description = "Tu van va gioi thieu bac si thu y uy tin", DurationMinutes = 30, Price = 0, IsActive = true }
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

            var branches = new List<Branch>
            {
                new Branch
                {
                    BranchName = "PetCare - Chi nhanh Ha Noi",
                    Address = "123 Lang Ha, Ba Dinh, Ha Noi",
                    Phone = "024-1234-5678",
                    Email = "hanoi@petcare.com",
                    IsActive = true
                },
                new Branch
                {
                    BranchName = "PetCare - Chi nhanh TP.HCM",
                    Address = "456 Nguyen Hue, Quan 1, TP.HCM",
                    Phone = "028-8765-4321",
                    Email = "hcm@petcare.com",
                    IsActive = true
                }
            };
            await context.Branches.AddRangeAsync(branches);
            await context.SaveChangesAsync();

            var faqItems = new List<FaqItem>
            {
                new FaqItem
                {
                    Question = "PetCare cung cap nhung dich vu gi?",
                    Answer = "PetCare la nen tang ket noi cung cap dich vu grooming, spa, khach san thu cung va tu van suc khoe.",
                    Category = "Dich vu",
                    Keywords = new[] { "dich vu", "grooming", "spa", "khach san" },
                    IsActive = true
                },
                new FaqItem
                {
                    Question = "Toi nen cho thu cung an gi?",
                    Answer = "Nen cho thu cung an thuc an chuyen dung, can doi dinh duong theo do tuoi va giong loai.",
                    Category = "Dinh duong",
                    Keywords = new[] { "thuc an", "dinh duong", "che do an" },
                    IsActive = true
                }
            };
            await context.FaqItems.AddRangeAsync(faqItems);
            await context.SaveChangesAsync();

            var subscriptionPackages = new List<SubscriptionPackage>
            {
                new SubscriptionPackage
                {
                    Name = "Goi Mien Phi",
                    Description = "Theo doi ho so suc khoe co ban, hoan toan mien phi.",
                    Price = 0,
                    BillingCycle = "Month",
                    IsActive = true,
                    HasAIHealthTracking = false,
                    HasVaccinationTracking = false,
                    HasHealthReminders = false,
                    HasAIRecommendations = false,
                    HasNutritionalAnalysis = false,
                    HasEarlyDiseaseDetection = false,
                    HasPrioritySupport = false,
                    MaxPets = 1
                },
                new SubscriptionPackage
                {
                    Name = "Goi Premium",
                    Description = "Theo doi suc khoe AI va goi nhac cham soc nang cao.",
                    Price = 5000,
                    BillingCycle = "Month",
                    IsActive = true,
                    HasAIHealthTracking = true,
                    HasVaccinationTracking = true,
                    HasHealthReminders = true,
                    HasAIRecommendations = true,
                    HasNutritionalAnalysis = true,
                    HasEarlyDiseaseDetection = true,
                    HasPrioritySupport = true,
                    MaxPets = null
                }
            };
            await context.SubscriptionPackages.AddRangeAsync(subscriptionPackages);
            await context.SaveChangesAsync();
        }
        catch
        {
            throw;
        }
    }
}
