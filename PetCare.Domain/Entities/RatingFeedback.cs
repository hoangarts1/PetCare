namespace PetCare.Domain.Entities;

using PetCare.Domain.Common;

public class RatingFeedback : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public virtual Appointment Appointment { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
