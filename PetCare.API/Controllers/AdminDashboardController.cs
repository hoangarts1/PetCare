using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCare.Infrastructure.Data;

namespace PetCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly PetCareDbContext _context;

    public AdminDashboardController(PetCareDbContext context)
    {
        _context = context;
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueOverview()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = monthStart.AddMonths(1);

        var paidOrdersQuery = _context.Orders
            .AsNoTracking()
            .Where(order =>
                (order.PaymentStatus != null &&
                 (order.PaymentStatus.ToLower() == "paid" || order.PaymentStatus.ToLower() == "completed")) ||
                (order.PaymentMethod != null && order.PaymentMethod.ToLower() == "cod" &&
                 order.OrderStatus != null &&
                 (order.OrderStatus.ToLower() == "completed" || order.OrderStatus.ToLower() == "delivered")));

        var totalRevenue = await paidOrdersQuery.SumAsync(order => (decimal?)order.FinalAmount) ?? 0m;
        var paidOrders = await paidOrdersQuery.CountAsync();
        var paidRevenueThisMonth = await paidOrdersQuery
            .Where(order => order.OrderedAt >= monthStart && order.OrderedAt < nextMonthStart)
            .SumAsync(order => (decimal?)order.FinalAmount) ?? 0m;

        var totalOrders = await _context.Orders.AsNoTracking().CountAsync();

        return Ok(new
        {
            success = true,
            message = "Revenue overview retrieved successfully",
            data = new
            {
                totalRevenue,
                paidRevenueThisMonth,
                totalOrders,
                paidOrders,
                generatedAt = now
            }
        });
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = monthStart.AddMonths(1);

        var paidOrdersQuery = _context.Orders
            .AsNoTracking()
            .Where(order =>
                (order.PaymentStatus != null &&
                 (order.PaymentStatus.ToLower() == "paid" || order.PaymentStatus.ToLower() == "completed")) ||
                (order.PaymentMethod != null && order.PaymentMethod.ToLower() == "cod" &&
                 order.OrderStatus != null &&
                 (order.OrderStatus.ToLower() == "completed" || order.OrderStatus.ToLower() == "delivered")));

        var totalUsers = await _context.Users.AsNoTracking().CountAsync();
        var activeUsers = await _context.Users.AsNoTracking().CountAsync(u => u.IsActive);
        var newUsersThisMonth = await _context.Users.AsNoTracking()
            .CountAsync(u => u.CreatedAt >= monthStart && u.CreatedAt < nextMonthStart);

        var totalProducts = await _context.Products.AsNoTracking().CountAsync();
        var activeProducts = await _context.Products.AsNoTracking().CountAsync(p => p.IsActive);
        var lowStockProducts = await _context.Products.AsNoTracking().CountAsync(p => p.IsActive && p.StockQuantity <= 10);

        var totalRevenue = await paidOrdersQuery.SumAsync(order => (decimal?)order.FinalAmount) ?? 0m;
        var paidOrders = await paidOrdersQuery.CountAsync();
        var paidRevenueThisMonth = await paidOrdersQuery
            .Where(order => order.OrderedAt >= monthStart && order.OrderedAt < nextMonthStart)
            .SumAsync(order => (decimal?)order.FinalAmount) ?? 0m;
        var totalOrders = await _context.Orders.AsNoTracking().CountAsync();

        var recentUsers = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                RoleName = u.Role != null ? u.Role.RoleName : null,
                u.IsActive,
                u.CreatedAt
            })
            .ToListAsync();

        var lowStockProductsList = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.StockQuantity <= 10)
            .OrderBy(p => p.StockQuantity)
            .ThenBy(p => p.ProductName)
            .Take(5)
            .Select(p => new
            {
                p.Id,
                p.ProductName,
                p.StockQuantity,
                CategoryName = p.Category != null ? p.Category.CategoryName : null,
                p.IsActive,
                p.Price,
                p.SalePrice
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            message = "Admin overview retrieved successfully",
            data = new
            {
                totals = new
                {
                    users = totalUsers,
                    activeUsers,
                    newUsersThisMonth,
                    products = totalProducts,
                    activeProducts,
                    lowStockProducts,
                    totalRevenue,
                    revenueThisMonth = paidRevenueThisMonth,
                    totalOrders,
                    paidOrders
                },
                recentUsers,
                lowStockProducts = lowStockProductsList,
                generatedAt = now
            }
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 200)
        {
            return BadRequest(new { success = false, message = "Invalid pagination parameters" });
        }

        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .OrderByDescending(u => u.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                RoleName = u.Role != null ? u.Role.RoleName : null,
                u.IsActive,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            message = "Users retrieved successfully",
            data = new
            {
                items,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        });
    }

}