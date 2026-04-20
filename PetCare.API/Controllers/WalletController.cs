using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCare.Domain.Entities;
using PetCare.Infrastructure.Data;

namespace PetCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly PetCareDbContext _context;

    public WalletController(PetCareDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyWallet()
    {
        var userId = GetUserId();
        var wallet = await GetOrCreateWalletAsync(userId);

        return Ok(new
        {
            success = true,
            message = "Wallet retrieved successfully",
            data = new
            {
                walletId = wallet.Id,
                userId = wallet.UserId,
                balance = wallet.Balance,
                pendingWithdrawal = wallet.PendingWithdrawal,
                availableBalance = wallet.Balance - wallet.PendingWithdrawal,
                updatedAt = wallet.UpdatedAt,
                createdAt = wallet.CreatedAt
            }
        });
    }

    [HttpGet("me/transactions")]
    public async Task<IActionResult> GetMyTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new { success = false, message = "Invalid pagination parameters" });
        }

        var userId = GetUserId();
        var wallet = await GetOrCreateWalletAsync(userId);

        var query = _context.WalletTransactions
            .AsNoTracking()
            .Where(transaction => transaction.WalletId == wallet.Id)
            .OrderByDescending(transaction => transaction.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(transaction => new
            {
                transaction.Id,
                transaction.TransactionType,
                transaction.Status,
                transaction.Amount,
                transaction.BalanceBefore,
                transaction.BalanceAfter,
                transaction.ReferenceType,
                transaction.ReferenceId,
                transaction.Description,
                transaction.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            message = "Wallet transactions retrieved successfully",
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

    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] WalletAmountRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { success = false, message = "Amount must be greater than 0" });
        }

        var userId = GetUserId();
        var wallet = await GetOrCreateWalletAsync(userId);

        var amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        var before = wallet.Balance;
        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _context.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = wallet.Id,
            UserId = userId,
            TransactionType = "deposit",
            Status = "completed",
            Amount = amount,
            BalanceBefore = before,
            BalanceAfter = wallet.Balance,
            Description = string.IsNullOrWhiteSpace(request.Note) ? "Wallet top-up" : request.Note!.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Deposit successful",
            data = new
            {
                walletId = wallet.Id,
                balance = wallet.Balance,
                availableBalance = wallet.Balance - wallet.PendingWithdrawal
            }
        });
    }

    [HttpPost("withdrawals")]
    public async Task<IActionResult> CreateWithdrawalRequest([FromBody] WalletAmountRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { success = false, message = "Amount must be greater than 0" });
        }

        var userId = GetUserId();
        var wallet = await GetOrCreateWalletAsync(userId);
        var amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        var available = wallet.Balance - wallet.PendingWithdrawal;

        if (available < amount)
        {
            return BadRequest(new
            {
                success = false,
                message = "Insufficient wallet balance. Please top up before creating a withdrawal request.",
                data = new
                {
                    wallet.Balance,
                    wallet.PendingWithdrawal,
                    availableBalance = available,
                    requestedAmount = amount
                }
            });
        }

        wallet.PendingWithdrawal += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var requestEntity = new WalletWithdrawalRequest
        {
            WalletId = wallet.Id,
            UserId = userId,
            Amount = amount,
            Status = "pending",
            Note = request.Note?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.WalletWithdrawalRequests.AddAsync(requestEntity);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Withdrawal request created and waiting for staff confirmation",
            data = new
            {
                requestId = requestEntity.Id,
                requestEntity.Amount,
                requestEntity.Status,
                walletBalance = wallet.Balance,
                wallet.PendingWithdrawal,
                availableBalance = wallet.Balance - wallet.PendingWithdrawal
            }
        });
    }

    [HttpPost("pay-service")]
    public async Task<IActionResult> PayService([FromBody] PayServiceRequest request)
    {
        if (request.AppointmentId == Guid.Empty)
        {
            return BadRequest(new { success = false, message = "AppointmentId is required" });
        }

        var userId = GetUserId();
        var wallet = await GetOrCreateWalletAsync(userId);

        var appointment = await _context.Appointments.FirstOrDefaultAsync(item => item.Id == request.AppointmentId && item.UserId == userId);
        if (appointment == null)
        {
            return NotFound(new { success = false, message = "Appointment not found" });
        }

        if (!string.Equals(appointment.AppointmentStatus, "completed", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Service can only be paid after appointment is completed" });
        }

        var serviceAmount = Math.Round(appointment.TotalAmount ?? 0m, 2, MidpointRounding.AwayFromZero);
        if (serviceAmount <= 0)
        {
            return BadRequest(new { success = false, message = "Appointment has no payable amount" });
        }

        var hasPaid = await _context.WalletTransactions
            .AsNoTracking()
            .AnyAsync(item =>
                item.UserId == userId
                && item.TransactionType == "service_payment"
                && item.ReferenceType == "appointment"
                && item.ReferenceId == appointment.Id
                && item.Status == "completed");

        if (hasPaid)
        {
            return BadRequest(new { success = false, message = "This appointment has already been paid by wallet" });
        }

        var availableBalance = wallet.Balance - wallet.PendingWithdrawal;
        if (availableBalance < serviceAmount)
        {
            return BadRequest(new
            {
                success = false,
                message = "Insufficient wallet balance. Please top up more to pay for this service.",
                data = new
                {
                    walletBalance = wallet.Balance,
                    wallet.PendingWithdrawal,
                    availableBalance,
                    requiredAmount = serviceAmount
                }
            });
        }

        var before = wallet.Balance;
        wallet.Balance -= serviceAmount;
        wallet.UpdatedAt = DateTime.UtcNow;

        appointment.Notes = string.IsNullOrWhiteSpace(appointment.Notes)
            ? $"Wallet payment completed at {DateTime.UtcNow:O}"
            : $"{appointment.Notes}\nWallet payment completed at {DateTime.UtcNow:O}";
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = wallet.Id,
            UserId = userId,
            TransactionType = "service_payment",
            Status = "completed",
            Amount = serviceAmount,
            BalanceBefore = before,
            BalanceAfter = wallet.Balance,
            ReferenceType = "appointment",
            ReferenceId = appointment.Id,
            Description = $"Wallet payment for appointment {appointment.BillNumber ?? appointment.Id.ToString()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Service paid successfully using wallet",
            data = new
            {
                appointmentId = appointment.Id,
                paidAmount = serviceAmount,
                wallet.Balance,
                wallet.PendingWithdrawal,
                availableBalance = wallet.Balance - wallet.PendingWithdrawal
            }
        });
    }

    [HttpGet("withdrawals")]
    [Authorize(Roles = "Admin,admin,Staff,staff")]
    public async Task<IActionResult> GetWithdrawalRequests([FromQuery] string? status = "pending", [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 200)
        {
            return BadRequest(new { success = false, message = "Invalid pagination parameters" });
        }

        var query = _context.WalletWithdrawalRequests
            .AsNoTracking()
            .Include(request => request.User)
            .OrderByDescending(request => request.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToLowerInvariant();
            query = query.Where(request => request.Status.ToLower() == normalized);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(request => new
            {
                request.Id,
                request.WalletId,
                request.UserId,
                customerName = request.User.FullName,
                customerEmail = request.User.Email,
                request.Amount,
                request.Status,
                request.Note,
                request.RejectionReason,
                request.ReviewedBy,
                request.ReviewedAt,
                request.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            message = "Withdrawal requests retrieved successfully",
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

    [HttpPost("withdrawals/{requestId:guid}/approve")]
    [Authorize(Roles = "Admin,admin,Staff,staff")]
    public async Task<IActionResult> ApproveWithdrawalRequest(Guid requestId)
    {
        var reviewerId = GetUserId();
        var request = await _context.WalletWithdrawalRequests
            .Include(withdrawal => withdrawal.Wallet)
            .FirstOrDefaultAsync(withdrawal => withdrawal.Id == requestId);

        if (request == null)
        {
            return NotFound(new { success = false, message = "Withdrawal request not found" });
        }

        if (!string.Equals(request.Status, "pending", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Only pending withdrawal can be approved" });
        }

        var walletEntity = request.Wallet;
        if (walletEntity.PendingWithdrawal < request.Amount || walletEntity.Balance < request.Amount)
        {
            return BadRequest(new { success = false, message = "Wallet state is invalid for approval" });
        }

        var before = walletEntity.Balance;
        walletEntity.PendingWithdrawal -= request.Amount;
        walletEntity.Balance -= request.Amount;
        walletEntity.UpdatedAt = DateTime.UtcNow;

        request.Status = "approved";
        request.ReviewedBy = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = walletEntity.Id,
            UserId = request.UserId,
            TransactionType = "withdraw",
            Status = "completed",
            Amount = request.Amount,
            BalanceBefore = before,
            BalanceAfter = walletEntity.Balance,
            ReferenceType = "wallet_withdrawal_request",
            ReferenceId = request.Id,
            Description = "Withdrawal approved by staff/admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Withdrawal approved successfully",
            data = new
            {
                requestId = request.Id,
                request.Status,
                balance = walletEntity.Balance,
                pendingWithdrawal = walletEntity.PendingWithdrawal,
                availableBalance = walletEntity.Balance - walletEntity.PendingWithdrawal
            }
        });
    }

    [HttpPost("withdrawals/{requestId:guid}/reject")]
    [Authorize(Roles = "Admin,admin,Staff,staff")]
    public async Task<IActionResult> RejectWithdrawalRequest(Guid requestId, [FromBody] RejectWithdrawalRequest requestBody)
    {
        var reviewerId = GetUserId();
        var request = await _context.WalletWithdrawalRequests
            .Include(withdrawal => withdrawal.Wallet)
            .FirstOrDefaultAsync(withdrawal => withdrawal.Id == requestId);

        if (request == null)
        {
            return NotFound(new { success = false, message = "Withdrawal request not found" });
        }

        if (!string.Equals(request.Status, "pending", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Only pending withdrawal can be rejected" });
        }

        var walletEntity = request.Wallet;
        walletEntity.PendingWithdrawal = Math.Max(0m, walletEntity.PendingWithdrawal - request.Amount);
        walletEntity.UpdatedAt = DateTime.UtcNow;

        request.Status = "rejected";
        request.ReviewedBy = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        request.RejectionReason = requestBody.Reason?.Trim();
        request.UpdatedAt = DateTime.UtcNow;

        await _context.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = walletEntity.Id,
            UserId = request.UserId,
            TransactionType = "withdraw_rejected",
            Status = "completed",
            Amount = request.Amount,
            BalanceBefore = walletEntity.Balance,
            BalanceAfter = walletEntity.Balance,
            ReferenceType = "wallet_withdrawal_request",
            ReferenceId = request.Id,
            Description = string.IsNullOrWhiteSpace(requestBody.Reason)
                ? "Withdrawal rejected"
                : $"Withdrawal rejected: {requestBody.Reason.Trim()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Withdrawal rejected and funds are available again",
            data = new
            {
                requestId = request.Id,
                request.Status,
                balance = walletEntity.Balance,
                pendingWithdrawal = walletEntity.PendingWithdrawal,
                availableBalance = walletEntity.Balance - walletEntity.PendingWithdrawal
            }
        });
    }

    private async Task<Wallet> GetOrCreateWalletAsync(Guid userId)
    {
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet != null)
        {
            return wallet;
        }

        wallet = new Wallet
        {
            UserId = userId,
            Balance = 0m,
            PendingWithdrawal = 0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();
        return wallet;
    }

    private Guid GetUserId()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(rawUserId, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }

        return userId;
    }
}

public class WalletAmountRequest
{
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class RejectWithdrawalRequest
{
    public string? Reason { get; set; }
}

public class PayServiceRequest
{
    public Guid AppointmentId { get; set; }
}
