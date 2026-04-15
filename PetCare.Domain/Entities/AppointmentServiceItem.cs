namespace PetCare.Domain.Entities;

using PetCare.Domain.Common;

public class AppointmentServiceItem : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid ServiceId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    // Navigation properties
    public virtual Appointment Appointment { get; set; } = null!;
    public virtual Service Service { get; set; } = null!;
}
