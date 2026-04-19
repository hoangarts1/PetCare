namespace PetCare.Domain.Entities;

using PetCare.Domain.Common;

public class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public Guid? RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public string PasswordHash { get; set; } = string.Empty;

    // Navigation properties
    public virtual Role? Role { get; set; }
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<RatingFeedback> RatingFeedbacks { get; set; } = new List<RatingFeedback>();
}
