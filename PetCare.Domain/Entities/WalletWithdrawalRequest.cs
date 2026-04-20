namespace PetCare.Domain.Entities;

using PetCare.Domain.Common;

public class WalletWithdrawalRequest : AuditableEntity
{
    public Guid WalletId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "pending";
    public string? Note { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual User? ReviewedByUser { get; set; }
}
