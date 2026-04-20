namespace PetCare.Domain.Entities;

using PetCare.Domain.Common;

public class Wallet : AuditableEntity
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
    public decimal PendingWithdrawal { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    public virtual ICollection<WalletWithdrawalRequest> WithdrawalRequests { get; set; } = new List<WalletWithdrawalRequest>();
}
