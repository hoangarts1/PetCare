using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCare.Application.DTOs.Order;
using PetCare.Domain.Entities;
using PetCare.Infrastructure.Data;

namespace PetCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CheckoutController : ControllerBase
{
    private const decimal MembershipDiscountRate = 0.10m;
    private const int WalletOrderRefundWindowDays = 3;
    private const string StaffAdminRoles = "Staff,staff,Admin,admin";

    private readonly PetCareDbContext _context;

    public CheckoutController(PetCareDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetUserId();

        var cartItems = await _context.CartItems
            .AsNoTracking()
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        var items = cartItems.Select(c =>
        {
            var unitPrice = GetEffectiveUnitPrice(c.Product);
            return new
            {
                c.Id,
                c.ProductId,
                ProductName = c.Product.ProductName,
                c.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * c.Quantity
            };
        }).ToList();

        var totalAmount = items.Sum(i => i.TotalPrice);
        var hasActiveMembership = await HasActiveMembershipAsync(userId);
        var membershipDiscountAmount = hasActiveMembership
            ? Math.Round(totalAmount * MembershipDiscountRate, 0, MidpointRounding.AwayFromZero)
            : 0m;

        var discountAmount = membershipDiscountAmount;
        var finalAmount = Math.Max(0m, totalAmount - discountAmount);

        return Ok(new
        {
            success = true,
            message = "Checkout summary retrieved successfully",
            data = new
            {
                items,
                totalAmount,
                hasMembershipDiscount = hasActiveMembership,
                membershipDiscountRate = hasActiveMembership ? MembershipDiscountRate : 0m,
                membershipDiscountAmount,
                discountAmount,
                shippingFee = 0m,
                finalAmount
            }
        });
    }

    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = GetUserId();

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(order => order.UserId == userId)
            .Include(order => order.OrderItems)
            .OrderByDescending(order => order.OrderedAt)
            .Select(order => new
            {
                order.Id,
                order.OrderNumber,
                order.OrderedAt,
                order.FinalAmount,
                order.OrderStatus,
                order.PaymentStatus,
                order.PaymentMethod,
                order.ShippingName,
                order.ShippingPhone,
                order.ShippingAddress,
                Note = order.Notes,
                Items = order.OrderItems
                    .OrderBy(item => item.CreatedAt)
                    .Select(item => new
                    {
                        item.Id,
                        item.ProductId,
                        item.ProductName,
                        item.Quantity,
                        item.UnitPrice,
                        item.TotalPrice
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            message = "Order history retrieved successfully",
            data = orders
        });
    }

    [HttpGet("all-orders")]
    [Authorize(Roles = StaffAdminRoles)]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .Include(order => order.User)
            .OrderByDescending(order => order.OrderedAt)
            .Select(order => new
            {
                order.Id,
                order.UserId,
                CustomerName = string.IsNullOrWhiteSpace(order.ShippingName)
                    ? order.User.FullName
                    : order.ShippingName,
                CustomerEmail = order.User.Email,
                order.OrderNumber,
                order.OrderedAt,
                order.FinalAmount,
                order.OrderStatus,
                order.PaymentStatus,
                order.PaymentMethod,
                order.ShippingName,
                order.ShippingPhone,
                order.ShippingAddress,
                Note = order.Notes,
                Items = order.OrderItems
                    .OrderBy(item => item.CreatedAt)
                    .Select(item => new
                    {
                        item.Id,
                        item.ProductId,
                        item.ProductName,
                        item.Quantity,
                        item.UnitPrice,
                        item.TotalPrice
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            message = "All orders retrieved successfully",
            data = orders
        });
    }

    [HttpPost("place-order")]
    public async Task<IActionResult> PlaceOrder([FromBody] CheckoutDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ShippingName)
            || string.IsNullOrWhiteSpace(dto.ShippingPhone)
            || string.IsNullOrWhiteSpace(dto.ShippingAddress))
        {
            return BadRequest(new
            {
                success = false,
                message = "Shipping name, phone, and address are required"
            });
        }

        var paymentMethod = (dto.PaymentMethod ?? "cod").Trim().ToLowerInvariant();
        if (paymentMethod == "payos")
        {
            return BadRequest(new
            {
                success = false,
                message = "PayOS payment for checkout orders is disabled. Please use COD or wallet."
            });
        }

        if (paymentMethod != "cod" && paymentMethod != "wallet")
        {
            return BadRequest(new
            {
                success = false,
                message = "Unsupported payment method"
            });
        }

        var userId = GetUserId();

        var cartItems = await _context.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        if (cartItems.Count == 0)
        {
            return BadRequest(new { success = false, message = "Cart is empty" });
        }

        foreach (var item in cartItems)
        {
            if (!item.Product.IsActive)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Product '{item.Product.ProductName}' is not available"
                });
            }

            if (item.Quantity > item.Product.StockQuantity)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Insufficient stock for '{item.Product.ProductName}'"
                });
            }
        }

        var now = DateTime.UtcNow;
        var orderNumber = $"ORD{now:yyyyMMddHHmmssfff}";

        var totalAmount = cartItems.Sum(i => GetEffectiveUnitPrice(i.Product) * i.Quantity);
        var hasActiveMembership = await HasActiveMembershipAsync(userId);
        var membershipDiscountAmount = hasActiveMembership
            ? Math.Round(totalAmount * MembershipDiscountRate, 0, MidpointRounding.AwayFromZero)
            : 0m;
        var discountAmount = membershipDiscountAmount;
        var finalAmount = totalAmount - discountAmount;

        Wallet? wallet = null;
        if (paymentMethod == "wallet")
        {
            wallet = await _context.Wallets.FirstOrDefaultAsync(item => item.UserId == userId);
            if (wallet == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Wallet not found. Please top up first."
                });
            }

            var availableBalance = wallet.Balance - wallet.PendingWithdrawal;
            if (availableBalance < finalAmount)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Insufficient wallet balance. Please top up more before paying with wallet.",
                    data = new
                    {
                        walletBalance = wallet.Balance,
                        wallet.PendingWithdrawal,
                        availableBalance,
                        requiredAmount = finalAmount
                    }
                });
            }
        }

        var order = new Order
        {
            UserId = userId,
            OrderNumber = orderNumber,
            OrderStatus = "pending",
            TotalAmount = totalAmount,
            FinalAmount = finalAmount,
            PaymentMethod = paymentMethod,
            PaymentStatus = paymentMethod == "cod" ? "pending" : paymentMethod == "wallet" ? "paid" : "unpaid",
            ShippingName = dto.ShippingName.Trim(),
            ShippingPhone = dto.ShippingPhone.Trim(),
            ShippingAddress = dto.ShippingAddress.Trim(),
            Notes = dto.Note?.Trim(),
            OrderedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _context.Orders.AddAsync(order);

        foreach (var cartItem in cartItems)
        {
            var unitPrice = GetEffectiveUnitPrice(cartItem.Product);

            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = cartItem.ProductId,
                ProductName = cartItem.Product.ProductName,
                Quantity = cartItem.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * cartItem.Quantity,
                CreatedAt = now
            };

            await _context.OrderItems.AddAsync(orderItem);

            cartItem.Product.StockQuantity -= cartItem.Quantity;
            cartItem.Product.UpdatedAt = now;
            _context.Products.Update(cartItem.Product);
        }

        {
            var payment = new Payment
            {
                OrderId = order.Id,
                UserId = userId,
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentMethod == "wallet" ? "completed" : "pending",
                Amount = finalAmount,
                PaidAt = paymentMethod == "wallet" ? DateTime.UtcNow : null,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _context.Payments.AddAsync(payment);

            if (paymentMethod == "wallet" && wallet != null)
            {
                var balanceBefore = wallet.Balance;
                wallet.Balance -= finalAmount;
                wallet.UpdatedAt = now;
                order.OrderStatus = "confirmed";

                await _context.WalletTransactions.AddAsync(new WalletTransaction
                {
                    WalletId = wallet.Id,
                    UserId = userId,
                    TransactionType = "purchase",
                    Status = "completed",
                    Amount = finalAmount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    ReferenceType = "order",
                    ReferenceId = order.Id,
                    Description = $"Wallet payment for order {order.OrderNumber}",
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        _context.CartItems.RemoveRange(cartItems);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = $"Checkout save failed: {ex.GetBaseException().Message}"
            });
        }

        return Ok(new
        {
            success = true,
            message = "Order placed successfully",
            data = new
            {
                order.Id,
                order.OrderNumber,
                order.TotalAmount,
                membershipDiscountAmount,
                order.FinalAmount,
                order.PaymentMethod,
                order.PaymentStatus,
                order.OrderStatus,
                order.OrderedAt
            }
        });
    }

    private async Task<bool> HasActiveMembershipAsync(Guid userId)
    {
        await Task.CompletedTask;
        return false;
    }

    [HttpPost("confirm-payment")]
    public async Task<IActionResult> ConfirmPayment([FromQuery] long? orderCode, [FromQuery] string? orderNumber)
    {
        await Task.CompletedTask;
        return BadRequest(new
        {
            success = false,
            message = "PayOS payment for checkout orders is disabled."
        });
    }

    [HttpPost("{orderId:guid}/request-wallet-refund")]
    public async Task<IActionResult> RequestWalletRefund(Guid orderId, [FromBody] RequestWalletRefundDto? dto)
    {
        var userId = GetUserId();
        var order = await _context.Orders
            .FirstOrDefaultAsync(item => item.Id == orderId && item.UserId == userId);

        if (order == null)
        {
            return NotFound(new { success = false, message = "Order not found" });
        }

        if (!string.Equals(order.PaymentMethod, "wallet", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Only wallet-paid orders can be refunded to wallet" });
        }

        if (!string.Equals(order.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(order.PaymentStatus, "completed", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Only paid orders can request refund" });
        }

        if (string.Equals(order.OrderStatus, "refund_requested", StringComparison.OrdinalIgnoreCase)
            || string.Equals(order.OrderStatus, "refunded", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Refund has already been requested or completed for this order" });
        }

        if (order.OrderedAt < DateTime.UtcNow.AddDays(-WalletOrderRefundWindowDays))
        {
            return BadRequest(new
            {
                success = false,
                message = $"Refund request is only allowed within {WalletOrderRefundWindowDays} days after purchase"
            });
        }

        order.OrderStatus = "refund_requested";
        order.UpdatedAt = DateTime.UtcNow;

        await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = "refund_requested",
            Notes = dto?.Reason?.Trim(),
            UpdatedBy = userId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Refund request submitted successfully and waiting for staff confirmation",
            data = new
            {
                order.Id,
                order.OrderNumber,
                order.OrderStatus,
                order.FinalAmount
            }
        });
    }

    [HttpPost("{orderId:guid}/approve-wallet-refund")]
    [Authorize(Roles = StaffAdminRoles)]
    public async Task<IActionResult> ApproveWalletRefund(Guid orderId)
    {
        var reviewerId = GetUserId();
        var order = await _context.Orders
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == orderId);

        if (order == null)
        {
            return NotFound(new { success = false, message = "Order not found" });
        }

        if (!string.Equals(order.PaymentMethod, "wallet", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Only wallet-paid orders can be refunded to wallet" });
        }

        if (!string.Equals(order.OrderStatus, "refund_requested", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Order is not in refund requested state" });
        }

        var wallet = await _context.Wallets.FirstOrDefaultAsync(item => item.UserId == order.UserId);
        if (wallet == null)
        {
            wallet = new Wallet
            {
                UserId = order.UserId,
                Balance = 0m,
                PendingWithdrawal = 0m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Wallets.AddAsync(wallet);
        }

        var before = wallet.Balance;
        wallet.Balance += order.FinalAmount;
        wallet.UpdatedAt = DateTime.UtcNow;

        order.OrderStatus = "refunded";
        order.PaymentStatus = "refunded";
        order.UpdatedAt = DateTime.UtcNow;

        var payment = await _context.Payments
            .FirstOrDefaultAsync(item => item.OrderId == order.Id);
        if (payment != null)
        {
            payment.PaymentStatus = "refunded";
            payment.RefundedAt = DateTime.UtcNow;
            payment.RefundAmount = order.FinalAmount;
            payment.RefundReason = "Refunded to wallet after staff confirmation";
            payment.UpdatedAt = DateTime.UtcNow;
        }

        await _context.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = wallet.Id,
            UserId = order.UserId,
            TransactionType = "order_refund",
            Status = "completed",
            Amount = order.FinalAmount,
            BalanceBefore = before,
            BalanceAfter = wallet.Balance,
            ReferenceType = "order",
            ReferenceId = order.Id,
            Description = $"Refund for order {order.OrderNumber}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = "refunded",
            Notes = "Refund approved by staff/admin and returned to wallet",
            UpdatedBy = reviewerId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Wallet refund approved successfully",
            data = new
            {
                order.Id,
                order.OrderNumber,
                refundAmount = order.FinalAmount,
                walletBalance = wallet.Balance,
                order.OrderStatus,
                order.PaymentStatus
            }
        });
    }

    [AllowAnonymous]
    [HttpPost("payos-webhook")]
    public async Task<IActionResult> PayOsWebhook()
    {
        await Task.CompletedTask;
        return BadRequest(new { success = false, message = "PayOS webhook for checkout orders is disabled." });
    }

    private static decimal GetEffectiveUnitPrice(Product product)
    {
        return product.Price;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }

        return userId;
    }

    public class RequestWalletRefundDto
    {
        public string? Reason { get; set; }
    }
}
