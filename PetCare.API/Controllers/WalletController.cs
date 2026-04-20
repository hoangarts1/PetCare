using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using PetCare.Domain.Entities;
using PetCare.Infrastructure.Data;

namespace PetCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly PetCareDbContext _context;
    private readonly PayOSClient? _payOS;
    private readonly bool _payOsConfigured;
    private readonly string _fallbackTopupReturnUrl;
    private readonly string _fallbackTopupCancelUrl;

    public WalletController(PetCareDbContext context, IConfiguration configuration)
    {
        _context = context;

        var clientId = GetFirstNonEmpty(
            configuration["PayOS:ClientId"],
            Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID"));

        var apiKey = GetFirstNonEmpty(
            configuration["PayOS:ApiKey"],
            Environment.GetEnvironmentVariable("PAYOS_API_KEY"));

        var checksumKey = GetFirstNonEmpty(
            configuration["PayOS:ChecksumKey"],
            Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY"));

        _payOsConfigured = !string.IsNullOrWhiteSpace(clientId)
            && !string.IsNullOrWhiteSpace(apiKey)
            && !string.IsNullOrWhiteSpace(checksumKey);

        if (_payOsConfigured)
        {
            _payOS = new PayOSClient(clientId!, apiKey!, checksumKey!);
        }

        _fallbackTopupReturnUrl = configuration["PayOS:WalletTopupReturnUrl"]
            ?? "https://pettsuba.live/vi/nap-thanh-cong";

        _fallbackTopupCancelUrl = configuration["PayOS:WalletTopupCancelUrl"]
            ?? "https://pettsuba.live/vi";
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

    [HttpPost("topup/create-payos-link")]
    public async Task<IActionResult> CreatePayOsTopupLink([FromBody] CreateWalletTopupRequest request)
    {
        if (!_payOsConfigured || _payOS == null)
        {
            return BadRequest(new { success = false, message = "PayOS is not configured on server" });
        }

        if (request.Amount < 1000)
        {
            return BadRequest(new { success = false, message = "Minimum top-up amount is 1,000 VND" });
        }

        var userId = GetUserId();
        var wallet = await GetOrCreateWalletAsync(userId);
        var amount = Math.Round(request.Amount, 0, MidpointRounding.AwayFromZero);
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var (returnUrl, cancelUrl) = BuildWalletTopupReturnUrls(request.ReturnBaseUrl);
        var description = BuildWalletTopupDescription(userId);

        var paymentRequest = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = decimal.ToInt32(amount),
            Description = description,
            ReturnUrl = $"{returnUrl}?orderCode={orderCode}&amount={amount}",
            CancelUrl = cancelUrl,
            Items = new List<PaymentLinkItem>
            {
                new()
                {
                    Name = "Wallet top-up",
                    Quantity = 1,
                    Price = decimal.ToInt32(amount)
                }
            }
        };

        string checkoutUrl;
        try
        {
            var link = await _payOS.PaymentRequests.CreateAsync(paymentRequest);
            checkoutUrl = link.CheckoutUrl;
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"PayOS error: {ex.Message}" });
        }

        await _context.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = wallet.Id,
            UserId = userId,
            TransactionType = "deposit",
            Status = "pending",
            Amount = amount,
            BalanceBefore = wallet.Balance,
            BalanceAfter = wallet.Balance,
            ReferenceType = "wallet_topup",
            ReferenceId = Guid.NewGuid(),
            Description = JsonSerializer.Serialize(new WalletTopupMeta
            {
                OrderCode = orderCode,
                CheckoutUrl = checkoutUrl,
                Note = request.Note?.Trim()
            }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Top-up QR created successfully",
            data = new
            {
                orderCode,
                amount,
                points = amount,
                paymentUrl = checkoutUrl
            }
        });
    }

    [HttpPost("topup/confirm")]
    public async Task<IActionResult> ConfirmTopup([FromQuery] long orderCode)
    {
        if (orderCode <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid orderCode" });
        }

        var userId = GetUserId();
        var transaction = await _context.WalletTransactions
            .Include(item => item.Wallet)
            .Where(item => item.UserId == userId && item.ReferenceType == "wallet_topup")
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(item => TryGetTopupOrderCode(item.Description) == orderCode);

        if (transaction == null)
        {
            return NotFound(new { success = false, message = "Top-up transaction not found" });
        }

        if (string.Equals(transaction.Status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new
            {
                success = true,
                message = "Top-up was already confirmed",
                data = new
                {
                    orderCode,
                    transaction.Amount,
                    points = transaction.Amount,
                    balance = transaction.Wallet.Balance,
                    availableBalance = transaction.Wallet.Balance - transaction.Wallet.PendingWithdrawal,
                    alreadyConfirmed = true
                }
            });
        }

        var wallet = transaction.Wallet;
        var before = wallet.Balance;
        wallet.Balance += transaction.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        transaction.Status = "completed";
        transaction.BalanceBefore = before;
        transaction.BalanceAfter = wallet.Balance;
        transaction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Top-up confirmed successfully",
            data = new
            {
                orderCode,
                transaction.Amount,
                points = transaction.Amount,
                balance = wallet.Balance,
                availableBalance = wallet.Balance - wallet.PendingWithdrawal,
                alreadyConfirmed = false
            }
        });
    }

    [AllowAnonymous]
    [HttpPost("payos-webhook")]
    public async Task<IActionResult> PayOsWebhook([FromBody] PayOsWebhookRequest webhook)
    {
        if (!_payOsConfigured || _payOS == null)
        {
            return Ok(new { success = false, message = "PayOS not configured" });
        }

        try
        {
            var sdkWebhook = new Webhook
            {
                Code = webhook.Code,
                Description = webhook.Desc,
                Success = webhook.Success,
                Signature = webhook.Signature,
                Data = new WebhookData
                {
                    OrderCode = webhook.Data?.OrderCode ?? 0,
                    Amount = webhook.Data?.Amount ?? 0,
                    Description = webhook.Data?.Description ?? string.Empty,
                    AccountNumber = webhook.Data?.AccountNumber ?? string.Empty,
                    Reference = webhook.Data?.Reference ?? string.Empty,
                    TransactionDateTime = webhook.Data?.TransactionDateTime ?? string.Empty,
                    Currency = webhook.Data?.Currency ?? string.Empty,
                    PaymentLinkId = webhook.Data?.PaymentLinkId ?? string.Empty,
                    Code = webhook.Data?.Code ?? string.Empty,
                    CounterAccountBankId = webhook.Data?.CounterAccountBankId ?? string.Empty,
                    CounterAccountBankName = webhook.Data?.CounterAccountBankName ?? string.Empty,
                    CounterAccountName = webhook.Data?.CounterAccountName ?? string.Empty,
                    CounterAccountNumber = webhook.Data?.CounterAccountNumber ?? string.Empty,
                    VirtualAccountName = webhook.Data?.VirtualAccountName ?? string.Empty,
                    VirtualAccountNumber = webhook.Data?.VirtualAccountNumber ?? string.Empty
                }
            };

            var verified = await _payOS.Webhooks.VerifyAsync(sdkWebhook);
            var orderCode = verified.OrderCode;

            var transaction = await _context.WalletTransactions
                .Include(item => item.Wallet)
                .Where(item => item.ReferenceType == "wallet_topup")
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync(item => TryGetTopupOrderCode(item.Description) == orderCode);

            if (transaction == null)
            {
                return Ok(new { success = false, message = "Top-up transaction not found" });
            }

            if (string.Equals(transaction.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { success = true, message = "Top-up already completed" });
            }

            if (verified.Code == "00" && webhook.Success)
            {
                var wallet = transaction.Wallet;
                var before = wallet.Balance;
                wallet.Balance += transaction.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;

                transaction.Status = "completed";
                transaction.BalanceBefore = before;
                transaction.BalanceAfter = wallet.Balance;
                transaction.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                transaction.Status = "failed";
                transaction.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("withdrawals")]
    public async Task<IActionResult> CreateWithdrawalRequest([FromBody] WalletWithdrawalCreateRequest request)
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
            Note = JsonSerializer.Serialize(new WalletWithdrawalMeta
            {
                AccountHolder = request.AccountHolder?.Trim(),
                AccountNumber = request.AccountNumber?.Trim(),
                BankName = request.BankName?.Trim(),
                Note = request.Note?.Trim()
            }),
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

    private (string returnUrl, string cancelUrl) BuildWalletTopupReturnUrls(string? returnBaseUrl)
    {
        if (Uri.TryCreate(returnBaseUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            var origin = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? string.Empty : ":" + uri.Port)}";
            return ($"{origin}/vi/nap-thanh-cong", $"{origin}/vi");
        }

        return (_fallbackTopupReturnUrl, _fallbackTopupCancelUrl);
    }

    private static string BuildWalletTopupDescription(Guid userId)
    {
        var compact = userId.ToString("N")[..8].ToUpperInvariant();
        var description = $"Topup {compact}";
        return description.Length <= 25 ? description : description[..25];
    }

    private static long? TryGetTopupOrderCode(string? rawDescription)
    {
        if (string.IsNullOrWhiteSpace(rawDescription))
        {
            return null;
        }

        try
        {
            var meta = JsonSerializer.Deserialize<WalletTopupMeta>(rawDescription);
            return meta?.OrderCode;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetFirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}

public class WalletAmountRequest
{
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class CreateWalletTopupRequest
{
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public string? ReturnBaseUrl { get; set; }
}

public class WalletWithdrawalCreateRequest
{
    public decimal Amount { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountHolder { get; set; }
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

public class PayOsWebhookRequest
{
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Signature { get; set; } = string.Empty;
    public PayOsWebhookData? Data { get; set; }
}

public class PayOsWebhookData
{
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public string? Reference { get; set; }
    public string? TransactionDateTime { get; set; }
    public string? Currency { get; set; }
    public string PaymentLinkId { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? CounterAccountBankId { get; set; }
    public string? CounterAccountBankName { get; set; }
    public string? CounterAccountName { get; set; }
    public string? CounterAccountNumber { get; set; }
    public string? VirtualAccountName { get; set; }
    public string? VirtualAccountNumber { get; set; }
}

public class WalletTopupMeta
{
    public long OrderCode { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? Note { get; set; }
}

public class WalletWithdrawalMeta
{
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountHolder { get; set; }
    public string? Note { get; set; }
}
