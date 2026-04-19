using System.ComponentModel.DataAnnotations;

namespace PetCare.Application.DTOs.Appointment;

public class CreateAppointmentDto
{
    public string? Pet { get; set; }

    public Guid? ServiceId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string AppointmentType { get; set; } = string.Empty;
    
    [Required]
    public DateTime AppointmentDate { get; set; }
    
    [Required]
    public TimeSpan StartTime { get; set; }
    
    [Required]
    public TimeSpan EndTime { get; set; }
    
    public string? Notes { get; set; }
}

public class UpdateAppointmentDto
{
    public string? Pet { get; set; }
    public Guid? ServiceId { get; set; }
    public string? AppointmentType { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Notes { get; set; }
}

public class AppointmentResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Pet { get; set; }
    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public decimal? ServicePrice { get; set; }
    public string AppointmentType { get; set; } = string.Empty;
    public string AppointmentStatus { get; set; } = string.Empty;
    public Guid? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public string? CheckInCode { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? BillNumber { get; set; }
    public decimal? TotalAmount { get; set; }
    public List<AppointmentStatusHistoryDto> StatusHistory { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateAppointmentStatusDto
{
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;

    public string? MedicalNotes { get; set; }

    public string? CancellationReason { get; set; }
}

public class ConfirmAppointmentDto
{
    public string? Notes { get; set; }
}

public class CheckInAppointmentDto
{
    [Required]
    [StringLength(5, MinimumLength = 4)]
    public string CheckInCode { get; set; } = string.Empty;
}

public class StartAppointmentServiceDto
{
    public List<StartAppointmentServiceItemDto> Services { get; set; } = new();
    public string? Notes { get; set; }
}

public class StartAppointmentServiceItemDto
{
    [Required]
    public Guid ServiceId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;
}

public class CompleteAppointmentDto
{
    public string? Notes { get; set; }
}

public class CreateRatingFeedbackDto
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }
}

public class RatingFeedbackResponseDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AppointmentServiceItemResponseDto
{
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class AppointmentStatusHistoryDto
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AppointmentBillDto
{
    public Guid AppointmentId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Pet { get; set; }
    public List<AppointmentServiceItemResponseDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

public class ServiceListItemDto
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
}
