namespace PetCare.Domain.Entities;

using PetCare.Domain.Common;

public class Appointment : AuditableEntity
{
    public Guid UserId { get; set; }
    public string? Pet { get; set; }
    public Guid? ServiceId { get; set; }
    public string AppointmentType { get; set; } = string.Empty;
    public string AppointmentStatus { get; set; } = "pending";
    public Guid? AssignedStaffId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? ServiceAddress { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public string? CheckInCode { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? BillNumber { get; set; }
    public decimal? TotalAmount { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Service? Service { get; set; }
    public virtual User? AssignedStaff { get; set; }
    public virtual ICollection<AppointmentStatusHistory> StatusHistory { get; set; } = new List<AppointmentStatusHistory>();
}
