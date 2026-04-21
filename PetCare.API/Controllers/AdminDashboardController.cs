using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCare.Domain.Entities;
using PetCare.Infrastructure.Data;
using System.Globalization;

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

        var paidOrdersQuery = GetPaidOrdersQuery();

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

        var paidOrdersQuery = GetPaidOrdersQuery();

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
                p.Price
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

    [HttpGet("revenue-report")]
    public async Task<IActionResult> GetRevenueReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string groupBy = "day")
    {
        var normalizedGroupBy = string.Equals(groupBy, "month", StringComparison.OrdinalIgnoreCase)
            ? "month"
            : "day";

        var now = DateTime.UtcNow;
        var defaultFrom = now.Date.AddDays(-29);
        var startDateUtc = NormalizeToUtcDate(from ?? defaultFrom);
        var endDateUtc = NormalizeToUtcDate(to ?? now.Date);

        if (startDateUtc > endDateUtc)
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid date range. 'from' must be less than or equal to 'to'."
            });
        }

        var queryEndExclusive = endDateUtc.AddDays(1);

        var paidOrdersInRange = GetPaidOrdersQuery()
            .Where(order => order.OrderedAt >= startDateUtc && order.OrderedAt < queryEndExclusive);

        var totalRevenue = await paidOrdersInRange.SumAsync(order => (decimal?)order.FinalAmount) ?? 0m;
        var paidOrders = await paidOrdersInRange.CountAsync();
        var totalOrders = await _context.Orders
            .AsNoTracking()
            .CountAsync(order => order.OrderedAt >= startDateUtc && order.OrderedAt < queryEndExclusive);

        var averageOrderValue = paidOrders == 0 ? 0m : Math.Round(totalRevenue / paidOrders, 2);

        var points = normalizedGroupBy == "month"
            ? await paidOrdersInRange
                .GroupBy(order => new { order.OrderedAt.Year, order.OrderedAt.Month })
                .Select(group => new
                {
                    periodStart = new DateTime(group.Key.Year, group.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    periodLabel = $"{group.Key.Month:D2}/{group.Key.Year}",
                    revenue = group.Sum(order => order.FinalAmount),
                    orderCount = group.Count()
                })
                .OrderBy(point => point.periodStart)
                .ToListAsync()
            : await paidOrdersInRange
                .GroupBy(order => order.OrderedAt.Date)
                .Select(group => new
                {
                    periodStart = DateTime.SpecifyKind(group.Key, DateTimeKind.Utc),
                    periodLabel = group.Key.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    revenue = group.Sum(order => order.FinalAmount),
                    orderCount = group.Count()
                })
                .OrderBy(point => point.periodStart)
                .ToListAsync();

        return Ok(new
        {
            success = true,
            message = "Revenue report retrieved successfully",
            data = new
            {
                from = startDateUtc,
                to = endDateUtc,
                groupBy = normalizedGroupBy,
                totalRevenue,
                paidOrders,
                totalOrders,
                averageOrderValue,
                points,
                generatedAt = now
            }
        });
    }

    [HttpGet("revenue/appointments")]
    public async Task<IActionResult> GetAppointmentRevenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var (startDateUtc, endDateUtc, error) = ValidateDateRange(from, to);
        if (error != null)
        {
            return BadRequest(error);
        }

        var queryEndExclusive = endDateUtc.AddDays(1);

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.AppointmentDate >= startDateUtc && appointment.AppointmentDate < queryEndExclusive)
            .Select(appointment => new
            {
                appointment.Id,
                appointment.AppointmentType,
                appointment.AppointmentStatus,
                appointment.AppointmentDate,
                appointment.TotalAmount
            })
            .ToListAsync();

        var completedAppointments = appointments.Where(appointment => IsCompletedAppointmentStatus(appointment.AppointmentStatus)).ToList();
        var pendingAppointments = appointments.Where(appointment => IsPendingAppointmentStatus(appointment.AppointmentStatus)).ToList();
        var cancelledAppointments = appointments.Where(appointment => IsCancelledAppointmentStatus(appointment.AppointmentStatus)).ToList();

        var completedRevenue = completedAppointments.Sum(appointment => appointment.TotalAmount ?? 0m);
        var pendingRevenue = pendingAppointments.Sum(appointment => appointment.TotalAmount ?? 0m);
        var cancelledRevenue = cancelledAppointments.Sum(appointment => appointment.TotalAmount ?? 0m);

        var revenueByService = appointments
            .GroupBy(appointment => string.IsNullOrWhiteSpace(appointment.AppointmentType) ? "Unknown" : appointment.AppointmentType)
            .Select(group => new
            {
                appointmentType = group.Key,
                completedCount = group.Count(appointment => IsCompletedAppointmentStatus(appointment.AppointmentStatus)),
                pendingCount = group.Count(appointment => IsPendingAppointmentStatus(appointment.AppointmentStatus)),
                cancelledCount = group.Count(appointment => IsCancelledAppointmentStatus(appointment.AppointmentStatus)),
                completedRevenue = group.Where(appointment => IsCompletedAppointmentStatus(appointment.AppointmentStatus)).Sum(appointment => appointment.TotalAmount ?? 0m),
                pendingRevenue = group.Where(appointment => IsPendingAppointmentStatus(appointment.AppointmentStatus)).Sum(appointment => appointment.TotalAmount ?? 0m),
                cancelledRevenue = group.Where(appointment => IsCancelledAppointmentStatus(appointment.AppointmentStatus)).Sum(appointment => appointment.TotalAmount ?? 0m)
            })
            .OrderByDescending(item => item.completedRevenue)
            .ToList();

        return Ok(new
        {
            success = true,
            message = "Appointment revenue retrieved successfully",
            data = new
            {
                from = startDateUtc,
                to = endDateUtc,
                totalAppointments = appointments.Count,
                completedAppointments = completedAppointments.Count,
                pendingAppointments = pendingAppointments.Count,
                cancelledAppointments = cancelledAppointments.Count,
                totalRevenue = completedRevenue,
                pendingRevenue,
                cancelledRevenue,
                revenueByService,
                generatedAt = DateTime.UtcNow
            }
        });
    }

    [HttpGet("revenue/total")]
    public async Task<IActionResult> GetTotalRevenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var (startDateUtc, endDateUtc, error) = ValidateDateRange(from, to);
        if (error != null)
        {
            return BadRequest(error);
        }

        var queryEndExclusive = endDateUtc.AddDays(1);

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(order => order.OrderedAt >= startDateUtc && order.OrderedAt < queryEndExclusive)
            .Select(order => new
            {
                order.OrderStatus,
                order.PaymentStatus,
                FinalAmount = EF.Property<decimal?>(order, nameof(Order.FinalAmount))
            })
            .ToListAsync();

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.AppointmentDate >= startDateUtc && appointment.AppointmentDate < queryEndExclusive)
            .Select(appointment => new
            {
                appointment.AppointmentStatus,
                appointment.TotalAmount
            })
            .ToListAsync();

        var productCompletedRevenue = orders
            .Where(order => IsCompletedOrderStatus(order.OrderStatus))
            .Sum(order => order.FinalAmount ?? 0m);
        var productRefundRevenue = orders
            .Where(order => IsRefundedOrderStatus(order.OrderStatus) || IsRefundedPaymentStatus(order.PaymentStatus))
            .Sum(order => order.FinalAmount ?? 0m);
        var productPendingRevenue = orders
            .Where(order => IsPendingOrderStatus(order.OrderStatus))
            .Sum(order => order.FinalAmount ?? 0m);
        var productNetRevenue = productCompletedRevenue - productRefundRevenue;

        var appointmentCompletedRevenue = appointments
            .Where(appointment => IsCompletedAppointmentStatus(appointment.AppointmentStatus))
            .Sum(appointment => appointment.TotalAmount ?? 0m);
        var appointmentPendingRevenue = appointments
            .Where(appointment => IsPendingAppointmentStatus(appointment.AppointmentStatus))
            .Sum(appointment => appointment.TotalAmount ?? 0m);
        var appointmentCancelledRevenue = appointments
            .Where(appointment => IsCancelledAppointmentStatus(appointment.AppointmentStatus))
            .Sum(appointment => appointment.TotalAmount ?? 0m);

        var totalGrossRevenue = productCompletedRevenue + productRefundRevenue + appointmentCompletedRevenue;
        var totalNetRevenue = productNetRevenue + appointmentCompletedRevenue;
        var totalPendingRevenue = productPendingRevenue + appointmentPendingRevenue;
        var totalCancelledRevenue = appointmentCancelledRevenue;

        return Ok(new
        {
            success = true,
            message = "Total revenue (products and appointments) retrieved successfully",
            data = new
            {
                from = startDateUtc,
                to = endDateUtc,
                totals = new
                {
                    grossRevenue = totalGrossRevenue,
                    netRevenue = totalNetRevenue,
                    pendingRevenue = totalPendingRevenue,
                    cancelledRevenue = totalCancelledRevenue
                },
                products = new
                {
                    completedRevenue = productCompletedRevenue,
                    refundRevenue = productRefundRevenue,
                    pendingRevenue = productPendingRevenue,
                    netRevenue = productNetRevenue
                },
                appointments = new
                {
                    completedRevenue = appointmentCompletedRevenue,
                    pendingRevenue = appointmentPendingRevenue,
                    cancelledRevenue = appointmentCancelledRevenue
                },
                generatedAt = DateTime.UtcNow
            }
        });
    }

    [HttpGet("reports/summary")]
    public async Task<IActionResult> GetSummaryReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var (startDateUtc, endDateUtc, error) = ValidateDateRange(from, to);
        if (error != null)
        {
            return BadRequest(error);
        }

        var queryEndExclusive = endDateUtc.AddDays(1);

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(order => order.OrderedAt >= startDateUtc && order.OrderedAt < queryEndExclusive)
            .Select(order => new
            {
                order.Id,
                order.UserId,
                order.FinalAmount,
                order.OrderedAt,
                order.OrderStatus,
                order.PaymentStatus,
                order.PaymentMethod
            })
            .ToListAsync();

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.AppointmentDate >= startDateUtc && appointment.AppointmentDate < queryEndExclusive)
            .Select(appointment => new
            {
                appointment.Id,
                appointment.UserId,
                appointment.AppointmentType,
                appointment.AppointmentStatus,
                appointment.AppointmentDate,
                appointment.TotalAmount
            })
            .ToListAsync();

        var completedOrders = orders.Where(order => IsCompletedOrderStatus(order.OrderStatus)).ToList();
        var refundedOrders = orders.Where(order => IsRefundedOrderStatus(order.OrderStatus) || IsRefundedPaymentStatus(order.PaymentStatus)).ToList();
        var cancelledOrders = orders.Where(order => IsCancelledOrderStatus(order.OrderStatus)).ToList();
        var pendingOrders = orders.Where(order => IsPendingOrderStatus(order.OrderStatus) && !IsCancelledOrderStatus(order.OrderStatus)).ToList();

        var productGrossRevenue = completedOrders.Sum(order => order.FinalAmount) + refundedOrders.Sum(order => order.FinalAmount);
        var productRefundRevenue = refundedOrders.Sum(order => order.FinalAmount);
        var productPendingRevenue = pendingOrders.Sum(order => order.FinalAmount);
        var productNetRevenue = completedOrders.Sum(order => order.FinalAmount) - productRefundRevenue;

        var completedAppointments = appointments.Where(appointment => IsCompletedAppointmentStatus(appointment.AppointmentStatus)).ToList();
        var pendingAppointments = appointments.Where(appointment => IsPendingAppointmentStatus(appointment.AppointmentStatus)).ToList();
        var cancelledAppointments = appointments.Where(appointment => IsCancelledAppointmentStatus(appointment.AppointmentStatus)).ToList();

        var serviceGrossRevenue = completedAppointments.Sum(appointment => appointment.TotalAmount ?? 0m);
        var servicePendingRevenue = pendingAppointments.Sum(appointment => appointment.TotalAmount ?? 0m);
        var serviceRefundRevenue = 0m;
        var serviceNetRevenue = serviceGrossRevenue - serviceRefundRevenue;

        var grossRevenue = productGrossRevenue + serviceGrossRevenue;
        var refundRevenue = productRefundRevenue + serviceRefundRevenue;
        var pendingRevenue = productPendingRevenue + servicePendingRevenue;
        var netRevenue = productNetRevenue + serviceNetRevenue;

        var totalOrders = orders.Count;
        var completedOrderCount = completedOrders.Count;
        var refundedOrderCount = refundedOrders.Count;
        var cancelledOrderCount = cancelledOrders.Count;

        var completionRate = totalOrders == 0 ? 0m : Math.Round(completedOrderCount * 100m / totalOrders, 2);
        var returnRate = totalOrders == 0 ? 0m : Math.Round(refundedOrderCount * 100m / totalOrders, 2);
        var cancellationRate = totalOrders == 0 ? 0m : Math.Round(cancelledOrderCount * 100m / totalOrders, 2);

        var totalCustomers = orders.Select(order => order.UserId)
            .Concat(appointments.Select(appointment => appointment.UserId))
            .Distinct()
            .Count();

        var revenueTrendByDay = orders
            .GroupBy(order => order.OrderedAt.Date)
            .Select(group =>
            {
                var completed = group.Where(order => IsCompletedOrderStatus(order.OrderStatus)).Sum(order => order.FinalAmount);
                var refunded = group.Where(order => IsRefundedOrderStatus(order.OrderStatus) || IsRefundedPaymentStatus(order.PaymentStatus)).Sum(order => order.FinalAmount);
                var net = completed - refunded;

                return new
                {
                    date = DateTime.SpecifyKind(group.Key, DateTimeKind.Utc),
                    grossRevenue = completed + refunded,
                    refundRevenue = refunded,
                    netRevenue = net
                };
            })
            .OrderBy(item => item.date)
            .ToList();

        return Ok(new
        {
            success = true,
            message = "Summary report retrieved successfully",
            data = new
            {
                from = startDateUtc,
                to = endDateUtc,
                overview = new
                {
                    grossRevenue,
                    netRevenue,
                    totalOrders,
                    totalCustomers,
                    returnRate,
                    completionRate
                },
                revenueGroups = new
                {
                    grossRevenue,
                    netRevenue,
                    refundRevenue,
                    pendingRevenue
                },
                orderGroups = new
                {
                    completed = completedOrderCount,
                    refunded = refundedOrderCount,
                    cancelled = cancelledOrderCount,
                    cancellationRate
                },
                revenueTrend = revenueTrendByDay,
                breakdown = new
                {
                    products = new
                    {
                        grossRevenue = productGrossRevenue,
                        netRevenue = productNetRevenue,
                        refundRevenue = productRefundRevenue,
                        pendingRevenue = productPendingRevenue
                    },
                    services = new
                    {
                        grossRevenue = serviceGrossRevenue,
                        netRevenue = serviceNetRevenue,
                        refundRevenue = serviceRefundRevenue,
                        pendingRevenue = servicePendingRevenue,
                        completedAppointments = completedAppointments.Count,
                        pendingAppointments = pendingAppointments.Count,
                        cancelledAppointments = cancelledAppointments.Count
                    }
                },
                generatedAt = DateTime.UtcNow
            }
        });
    }

    [HttpGet("reports/orders")]
    public async Task<IActionResult> GetOrderReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var (startDateUtc, endDateUtc, error) = ValidateDateRange(from, to);
        if (error != null)
        {
            return BadRequest(error);
        }

        var queryEndExclusive = endDateUtc.AddDays(1);

        var orders = await _context.Orders
            .AsNoTracking()
            .Include(order => order.User)
            .Where(order => order.OrderedAt >= startDateUtc && order.OrderedAt < queryEndExclusive)
            .ToListAsync();

        var completedOrders = orders.Where(order => IsCompletedOrderStatus(order.OrderStatus)).ToList();
        var refundedOrders = orders.Where(order => IsRefundedOrderStatus(order.OrderStatus) || IsRefundedPaymentStatus(order.PaymentStatus)).ToList();
        var cancelledOrders = orders.Where(order => IsCancelledOrderStatus(order.OrderStatus)).ToList();

        var totalOrders = orders.Count;
        var completionRate = totalOrders == 0 ? 0m : Math.Round(completedOrders.Count * 100m / totalOrders, 2);
        var returnRate = totalOrders == 0 ? 0m : Math.Round(refundedOrders.Count * 100m / totalOrders, 2);
        var cancellationRate = totalOrders == 0 ? 0m : Math.Round(cancelledOrders.Count * 100m / totalOrders, 2);

        var highestValueOrder = orders
            .OrderByDescending(order => order.FinalAmount)
            .Select(order => new
            {
                order.Id,
                order.OrderNumber,
                order.FinalAmount,
                order.OrderStatus,
                order.OrderedAt,
                userId = order.UserId,
                customerName = order.User?.FullName,
                customerEmail = order.User?.Email
            })
            .FirstOrDefault();

        var topCancelCustomer = cancelledOrders
            .GroupBy(order => new { order.UserId, CustomerName = order.User?.FullName, CustomerEmail = order.User?.Email })
            .Select(group => new
            {
                userId = group.Key.UserId,
                customerName = group.Key.CustomerName,
                customerEmail = group.Key.CustomerEmail,
                cancelledOrders = group.Count(),
                cancelledAmount = group.Sum(order => order.FinalAmount)
            })
            .OrderByDescending(item => item.cancelledOrders)
            .ThenByDescending(item => item.cancelledAmount)
            .FirstOrDefault();

        var topRefundedOrders = refundedOrders
            .OrderByDescending(order => order.FinalAmount)
            .Take(10)
            .Select(order => new
            {
                order.Id,
                order.OrderNumber,
                order.FinalAmount,
                order.OrderStatus,
                order.PaymentStatus,
                order.OrderedAt,
                userId = order.UserId,
                customerName = order.User?.FullName,
                customerEmail = order.User?.Email
            })
            .ToList();

        var refundableWindowDays = 3;
        var refundableOrders = completedOrders
            .Count(order => order.OrderedAt >= DateTime.UtcNow.AddDays(-refundableWindowDays));

        return Ok(new
        {
            success = true,
            message = "Order report retrieved successfully",
            data = new
            {
                from = startDateUtc,
                to = endDateUtc,
                overview = new
                {
                    totalOrders,
                    completed = completedOrders.Count,
                    refunded = refundedOrders.Count,
                    cancelled = cancelledOrders.Count,
                    completionRate,
                    returnRate,
                    cancellationRate
                },
                refundablePolicy = new
                {
                    refundableWindowDays,
                    refundableOrders
                },
                highestValueOrder,
                topCancelCustomer,
                topRefundedOrders,
                generatedAt = DateTime.UtcNow
            }
        });
    }

    [HttpGet("reports/products")]
    public async Task<IActionResult> GetProductReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var (startDateUtc, endDateUtc, error) = ValidateDateRange(from, to);
        if (error != null)
        {
            return BadRequest(error);
        }

        var queryEndExclusive = endDateUtc.AddDays(1);

        var orderItems = await _context.OrderItems
            .AsNoTracking()
            .Include(item => item.Order)
            .Include(item => item.Product)
                .ThenInclude(product => product.Category)
            .Where(item => item.Order.OrderedAt >= startDateUtc && item.Order.OrderedAt < queryEndExclusive)
            .ToListAsync();

        var orderItemsWithStatus = orderItems.Select(item => new
        {
            item.Id,
            item.ProductId,
            item.ProductName,
            item.Quantity,
            item.TotalPrice,
            CategoryName = item.Product?.Category?.CategoryName ?? "Uncategorized",
            OrderStatus = item.Order.OrderStatus,
            PaymentStatus = item.Order.PaymentStatus,
            UserId = item.Order.UserId,
            item.Order.OrderedAt
        }).ToList();

        var categoryRevenue = orderItemsWithStatus
            .GroupBy(item => item.CategoryName)
            .Select(group => new
            {
                categoryName = group.Key,
                completedRevenue = group.Where(item => IsCompletedOrderStatus(item.OrderStatus)).Sum(item => item.TotalPrice),
                refundedRevenue = group.Where(item => IsRefundedOrderStatus(item.OrderStatus) || IsRefundedPaymentStatus(item.PaymentStatus)).Sum(item => item.TotalPrice),
                pendingRevenue = group.Where(item => IsPendingOrderStatus(item.OrderStatus)).Sum(item => item.TotalPrice),
                totalQuantity = group.Sum(item => item.Quantity)
            })
            .OrderByDescending(item => item.completedRevenue)
            .ToList();

        var productRevenue = orderItemsWithStatus
            .GroupBy(item => new { item.ProductId, item.ProductName, item.CategoryName })
            .Select(group =>
            {
                var completedRevenue = group.Where(item => IsCompletedOrderStatus(item.OrderStatus)).Sum(item => item.TotalPrice);
                var refundedRevenue = group.Where(item => IsRefundedOrderStatus(item.OrderStatus) || IsRefundedPaymentStatus(item.PaymentStatus)).Sum(item => item.TotalPrice);
                var pendingRevenue = group.Where(item => IsPendingOrderStatus(item.OrderStatus)).Sum(item => item.TotalPrice);
                var soldQuantity = group.Where(item => IsCompletedOrderStatus(item.OrderStatus)).Sum(item => item.Quantity);

                return new
                {
                    productId = group.Key.ProductId,
                    productName = group.Key.ProductName,
                    categoryName = group.Key.CategoryName,
                    completedRevenue,
                    refundedRevenue,
                    pendingRevenue,
                    netRevenue = completedRevenue - refundedRevenue,
                    soldQuantity
                };
            })
            .OrderByDescending(item => item.netRevenue)
            .ToList();

        var topSellingProducts = productRevenue
            .OrderByDescending(item => item.soldQuantity)
            .ThenByDescending(item => item.netRevenue)
            .Take(10)
            .ToList();

        var topProductCustomer = orderItemsWithStatus
            .Where(item => IsCompletedOrderStatus(item.OrderStatus))
            .GroupBy(item => item.UserId)
            .Select(group => new
            {
                userId = group.Key,
                totalQuantity = group.Sum(item => item.Quantity),
                totalRevenue = group.Sum(item => item.TotalPrice)
            })
            .OrderByDescending(item => item.totalQuantity)
            .ThenByDescending(item => item.totalRevenue)
            .FirstOrDefault();

        var topProductCustomerInfo = topProductCustomer == null
            ? null
            : await _context.Users
                .AsNoTracking()
                .Where(user => user.Id == topProductCustomer.userId)
                .Select(user => new
                {
                    userId = user.Id,
                    customerName = user.FullName,
                    customerEmail = user.Email,
                    topProductCustomer.totalQuantity,
                    topProductCustomer.totalRevenue
                })
                .FirstOrDefaultAsync();

        return Ok(new
        {
            success = true,
            message = "Product report retrieved successfully",
            data = new
            {
                from = startDateUtc,
                to = endDateUtc,
                categoryRevenue,
                productRevenue,
                topSellingProducts,
                topProductCustomer = topProductCustomerInfo,
                generatedAt = DateTime.UtcNow
            }
        });
    }

    [HttpGet("reports/services")]
    public async Task<IActionResult> GetServiceReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var (startDateUtc, endDateUtc, error) = ValidateDateRange(from, to);
        if (error != null)
        {
            return BadRequest(error);
        }

        var queryEndExclusive = endDateUtc.AddDays(1);

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.AppointmentDate >= startDateUtc && appointment.AppointmentDate < queryEndExclusive)
            .Select(appointment => new
            {
                appointment.Id,
                appointment.UserId,
                appointment.AppointmentType,
                appointment.AppointmentStatus,
                appointment.TotalAmount,
                appointment.AppointmentDate
            })
            .ToListAsync();

        var serviceGroups = appointments
            .GroupBy(appointment => string.IsNullOrWhiteSpace(appointment.AppointmentType) ? "Unknown" : appointment.AppointmentType)
            .Select(group =>
            {
                var completedRevenue = group.Where(appointment => IsCompletedAppointmentStatus(appointment.AppointmentStatus)).Sum(appointment => appointment.TotalAmount ?? 0m);
                var pendingRevenue = group.Where(appointment => IsPendingAppointmentStatus(appointment.AppointmentStatus)).Sum(appointment => appointment.TotalAmount ?? 0m);
                var refundedRevenue = 0m;

                return new
                {
                    serviceName = group.Key,
                    bookingCount = group.Count(),
                    completedCount = group.Count(appointment => IsCompletedAppointmentStatus(appointment.AppointmentStatus)),
                    cancelledCount = group.Count(appointment => IsCancelledAppointmentStatus(appointment.AppointmentStatus)),
                    pendingCount = group.Count(appointment => IsPendingAppointmentStatus(appointment.AppointmentStatus)),
                    completedRevenue,
                    pendingRevenue,
                    refundedRevenue,
                    netRevenue = completedRevenue - refundedRevenue
                };
            })
            .OrderByDescending(item => item.netRevenue)
            .ToList();

        var topServiceCustomers = appointments
            .Where(appointment => IsCompletedAppointmentStatus(appointment.AppointmentStatus))
            .GroupBy(appointment => appointment.UserId)
            .Select(group => new
            {
                userId = group.Key,
                appointmentCount = group.Count(),
                totalSpent = group.Sum(appointment => appointment.TotalAmount ?? 0m)
            })
            .OrderByDescending(item => item.totalSpent)
            .ThenByDescending(item => item.appointmentCount)
            .Take(10)
            .ToList();

        var topServiceCustomerIds = topServiceCustomers.Select(item => item.userId).ToList();
        var customerMap = await _context.Users
            .AsNoTracking()
            .Where(user => topServiceCustomerIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => new { user.FullName, user.Email });

        var topServiceCustomerDetails = topServiceCustomers.Select(item => new
        {
            userId = item.userId,
            customerName = customerMap.TryGetValue(item.userId, out var user) ? user.FullName : null,
            customerEmail = customerMap.TryGetValue(item.userId, out user) ? user.Email : null,
            item.appointmentCount,
            item.totalSpent
        }).ToList();

        return Ok(new
        {
            success = true,
            message = "Service report retrieved successfully",
            data = new
            {
                from = startDateUtc,
                to = endDateUtc,
                services = serviceGroups,
                topServiceCustomers = topServiceCustomerDetails,
                note = "Service revenue is based on appointment TotalAmount and AppointmentType because appointment_service_items table has been removed.",
                generatedAt = DateTime.UtcNow
            }
        });
    }

    [HttpGet("reports/customers")]
    public async Task<IActionResult> GetCustomerReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var (startDateUtc, endDateUtc, error) = ValidateDateRange(from, to);
        if (error != null)
        {
            return BadRequest(error);
        }

        var queryEndExclusive = endDateUtc.AddDays(1);

        var orders = await _context.Orders
            .AsNoTracking()
            .Include(order => order.User)
            .Where(order => order.OrderedAt >= startDateUtc && order.OrderedAt < queryEndExclusive)
            .ToListAsync();

        var orderItems = await _context.OrderItems
            .AsNoTracking()
            .Include(item => item.Order)
            .Where(item => item.Order.OrderedAt >= startDateUtc && item.Order.OrderedAt < queryEndExclusive)
            .ToListAsync();

        var completedOrders = orders.Where(order => IsCompletedOrderStatus(order.OrderStatus)).ToList();
        var cancelledOrders = orders.Where(order => IsCancelledOrderStatus(order.OrderStatus)).ToList();

        var customerSpending = completedOrders
            .GroupBy(order => new { order.UserId, CustomerName = order.User?.FullName, CustomerEmail = order.User?.Email })
            .Select(group => new
            {
                userId = group.Key.UserId,
                customerName = group.Key.CustomerName,
                customerEmail = group.Key.CustomerEmail,
                orderCount = group.Count(),
                totalSpent = group.Sum(order => order.FinalAmount)
            })
            .OrderByDescending(item => item.totalSpent)
            .ThenByDescending(item => item.orderCount)
            .ToList();

        var customerByQuantity = orderItems
            .Where(item => IsCompletedOrderStatus(item.Order.OrderStatus))
            .GroupBy(item => item.Order.UserId)
            .Select(group => new
            {
                userId = group.Key,
                totalQuantity = group.Sum(item => item.Quantity),
                totalAmount = group.Sum(item => item.TotalPrice),
                orderCount = group.Select(item => item.OrderId).Distinct().Count()
            })
            .OrderByDescending(item => item.totalQuantity)
            .ThenByDescending(item => item.totalAmount)
            .ToList();

        var topQuantityUserIds = customerByQuantity.Take(10).Select(item => item.userId).ToList();
        var topQuantityUsers = await _context.Users
            .AsNoTracking()
            .Where(user => topQuantityUserIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => new { user.FullName, user.Email });

        var topByQuantity = customerByQuantity.Take(10).Select(item => new
        {
            userId = item.userId,
            customerName = topQuantityUsers.TryGetValue(item.userId, out var user) ? user.FullName : null,
            customerEmail = topQuantityUsers.TryGetValue(item.userId, out user) ? user.Email : null,
            item.orderCount,
            item.totalQuantity,
            item.totalAmount
        }).ToList();

        var topCancelCustomers = cancelledOrders
            .GroupBy(order => new { order.UserId, CustomerName = order.User?.FullName, CustomerEmail = order.User?.Email })
            .Select(group => new
            {
                userId = group.Key.UserId,
                customerName = group.Key.CustomerName,
                customerEmail = group.Key.CustomerEmail,
                cancelledOrders = group.Count(),
                cancelledValue = group.Sum(order => order.FinalAmount)
            })
            .OrderByDescending(item => item.cancelledOrders)
            .ThenByDescending(item => item.cancelledValue)
            .Take(10)
            .ToList();

        return Ok(new
        {
            success = true,
            message = "Customer report retrieved successfully",
            data = new
            {
                from = startDateUtc,
                to = endDateUtc,
                topSpenders = customerSpending.Take(10),
                topByOrderCount = customerSpending.OrderByDescending(item => item.orderCount).ThenByDescending(item => item.totalSpent).Take(10),
                topByQuantity,
                topCancelCustomers,
                generatedAt = DateTime.UtcNow
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

    private IQueryable<PetCare.Domain.Entities.Order> GetPaidOrdersQuery()
    {
        return _context.Orders
            .AsNoTracking()
            .Where(order =>
                (order.PaymentStatus != null &&
                 (order.PaymentStatus.ToLower() == "paid" || order.PaymentStatus.ToLower() == "completed")) ||
                (order.PaymentMethod != null && order.PaymentMethod.ToLower() == "cod" &&
                 order.OrderStatus != null &&
                 (order.OrderStatus.ToLower() == "completed" || order.OrderStatus.ToLower() == "delivered")));
    }

    private static DateTime NormalizeToUtcDate(DateTime date)
    {
        var normalized = date.Date;
        return normalized.Kind switch
        {
            DateTimeKind.Utc => normalized,
            DateTimeKind.Local => normalized.ToUniversalTime(),
            _ => DateTime.SpecifyKind(normalized, DateTimeKind.Utc)
        };
    }

    private static (DateTime startDateUtc, DateTime endDateUtc, object? error) ValidateDateRange(DateTime? from, DateTime? to)
    {
        var now = DateTime.UtcNow;
        var startDateUtc = NormalizeToUtcDate(from ?? now.Date.AddDays(-29));
        var endDateUtc = NormalizeToUtcDate(to ?? now.Date);

        if (startDateUtc > endDateUtc)
        {
            return (startDateUtc, endDateUtc, new
            {
                success = false,
                message = "Invalid date range. 'from' must be less than or equal to 'to'."
            });
        }

        return (startDateUtc, endDateUtc, null);
    }

    private static bool IsCompletedOrderStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "completed" or "delivered";
    }

    private static bool IsRefundedOrderStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "refunded" or "refund";
    }

    private static bool IsCancelledOrderStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "cancelled" or "canceled";
    }

    private static bool IsPendingOrderStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "pending" or "processing" or "confirmed";
    }

    private static bool IsRefundedPaymentStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "refunded" or "refund";
    }

    private static bool IsCompletedAppointmentStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "completed";
    }

    private static bool IsCancelledAppointmentStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "cancelled" or "canceled";
    }

    private static bool IsPendingAppointmentStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "pending" or "confirmed" or "checked-in" or "in-progress";
    }

}