using PetCare.Application.Common;
using PetCare.Application.DTOs.Appointment;

namespace PetCare.Application.Services.Interfaces;

public interface IAppointmentService
{
    Task<ServiceResult<IEnumerable<ServiceListItemDto>>> GetAvailableServicesAsync();
    Task<ServiceResult<AppointmentResponseDto>> CreateAppointmentAsync(CreateAppointmentDto dto, Guid userId);
    Task<ServiceResult<IEnumerable<AppointmentResponseDto>>> GetUserAppointmentsAsync(Guid userId);
    Task<ServiceResult<AppointmentResponseDto>> GetAppointmentByIdAsync(Guid appointmentId, Guid userId, string userRole);
    Task<ServiceResult<bool>> CancelAppointmentAsync(Guid appointmentId, Guid userId, string? cancellationReason);
    Task<ServiceResult<IEnumerable<AppointmentResponseDto>>> GetAllAppointmentsAsync(string? status, DateTime? date);
    Task<ServiceResult<AppointmentResponseDto>> UpdateAppointmentStatusAsync(Guid appointmentId, UpdateAppointmentStatusDto dto, Guid staffId);
    Task<ServiceResult<AppointmentResponseDto>> ConfirmAppointmentAsync(Guid appointmentId, Guid staffId, string? notes);
    Task<ServiceResult<AppointmentResponseDto>> CheckInAppointmentAsync(Guid appointmentId, string checkInCode, Guid staffId);
    Task<ServiceResult<AppointmentResponseDto>> StartServiceAsync(Guid appointmentId, StartAppointmentServiceDto dto, Guid staffId);
    Task<ServiceResult<AppointmentResponseDto>> CompleteAppointmentAsync(Guid appointmentId, Guid staffId, string? notes);
    Task<ServiceResult<AppointmentBillDto>> GetAppointmentBillAsync(Guid appointmentId, Guid userId, string userRole);
}
