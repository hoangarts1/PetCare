namespace PetCare.Domain.Entities;

using PetCare.Domain.Common;

public class WalletTransaction : AuditableEntity
{
    public Guid WalletId { get; set; }
    public Guid UserId { get; set; }
    public string TransactionType { get; set; } = "deposit";
    public string Status { get; set; } = "completed";
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Description { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
