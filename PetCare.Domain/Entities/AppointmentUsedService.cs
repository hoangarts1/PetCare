namespace PetCare.Domain.Entities;

using PetCare.Domain.Common;

public class AppointmentUsedService : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public virtual Appointment Appointment { get; set; } = null!;
    public virtual Service Service { get; set; } = null!;
}
