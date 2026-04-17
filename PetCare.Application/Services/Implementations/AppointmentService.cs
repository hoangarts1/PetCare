using PetCare.Application.Common;
using PetCare.Application.DTOs.Appointment;
using PetCare.Application.DTOs.Service;
using PetCare.Application.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using PetCare.Domain.Entities;
using PetCare.Domain.Interfaces;
using PetCare.Infrastructure.Repositories.Interfaces;
using System.Security.Cryptography;

namespace PetCare.Application.Services.Implementations;

public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public AppointmentService(IUnitOfWork unitOfWork, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;

    }

    public async Task<ServiceResult<IEnumerable<ServiceListItemDto>>> GetAvailableServicesAsync()
    {
        try
        {
            var services = await _unitOfWork.Services.GetActiveServicesAsync();
            var dtos = services
                .Where(s => !s.IsHomeService)
                .Select(s => new ServiceListItemDto
            {
                Id = s.Id,
                ServiceName = s.ServiceName,
                Description = s.Description,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                CategoryName = s.Category?.CategoryName
            });
            return ServiceResult<IEnumerable<ServiceListItemDto>>.SuccessResult(dtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<ServiceListItemDto>>.FailureResult($"Error retrieving services: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<ServiceDto>>> GetAllServicesAsync()
    {
        try
        {
            var services = await _unitOfWork.Repository<Service>()
                .Query()
                .Include(s => s.Category)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var dtos = services.Select(MapToServiceDto);
            return ServiceResult<IEnumerable<ServiceDto>>.SuccessResult(dtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<ServiceDto>>.FailureResult($"Error retrieving all services: {ex.Message}");
        }
    }

    public async Task<ServiceResult<ServiceDto>> GetServiceByIdAsync(Guid serviceId)
    {
        try
        {
            var service = await _unitOfWork.Repository<Service>()
                .Query()
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.Id == serviceId);

            if (service == null)
                return ServiceResult<ServiceDto>.FailureResult("Service not found");

            return ServiceResult<ServiceDto>.SuccessResult(MapToServiceDto(service));
        }
        catch (Exception ex)
        {
            return ServiceResult<ServiceDto>.FailureResult($"Error retrieving service: {ex.Message}");
        }
    }

    public async Task<ServiceResult<ServiceDto>> CreateServiceAsync(CreateServiceDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.ServiceName))
                return ServiceResult<ServiceDto>.FailureResult("Service name is required");

            if (dto.Price < 0)
                return ServiceResult<ServiceDto>.FailureResult("Price must be greater than or equal to 0");

            if (dto.DurationMinutes <= 0)
                return ServiceResult<ServiceDto>.FailureResult("DurationMinutes must be greater than 0");

            if (dto.CategoryId.HasValue)
            {
                var categoryExists = await _unitOfWork.Repository<ServiceCategory>().AnyAsync(c => c.Id == dto.CategoryId.Value);
                if (!categoryExists)
                    return ServiceResult<ServiceDto>.FailureResult("Service category not found");
            }

            var service = new Service
            {
                CategoryId = dto.CategoryId,
                ServiceName = dto.ServiceName.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                DurationMinutes = dto.DurationMinutes,
                Price = dto.Price,
                IsHomeService = dto.IsHomeService,
                IsActive = true
            };

            await _unitOfWork.Repository<Service>().AddAsync(service);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Repository<Service>()
                .Query()
                .Include(s => s.Category)
                .FirstAsync(s => s.Id == service.Id);

            return ServiceResult<ServiceDto>.SuccessResult(MapToServiceDto(created), "Service created successfully");
        }
        catch (Exception ex)
        {
            return ServiceResult<ServiceDto>.FailureResult($"Error creating service: {ex.Message}");
        }
    }

    public async Task<ServiceResult<ServiceDto>> UpdateServiceAsync(Guid serviceId, UpdateServiceDto dto)
    {
        try
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(serviceId);
            if (service == null)
                return ServiceResult<ServiceDto>.FailureResult("Service not found");

            if (!string.IsNullOrWhiteSpace(dto.ServiceName))
                service.ServiceName = dto.ServiceName.Trim();

            if (dto.Description != null)
                service.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            if (dto.Price.HasValue)
            {
                if (dto.Price.Value < 0)
                    return ServiceResult<ServiceDto>.FailureResult("Price must be greater than or equal to 0");

                service.Price = dto.Price.Value;
            }

            if (dto.DurationMinutes.HasValue)
            {
                if (dto.DurationMinutes.Value <= 0)
                    return ServiceResult<ServiceDto>.FailureResult("DurationMinutes must be greater than 0");

                service.DurationMinutes = dto.DurationMinutes.Value;
            }

            if (dto.IsActive.HasValue)
                service.IsActive = dto.IsActive.Value;

            await _unitOfWork.Repository<Service>().UpdateAsync(service);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.Repository<Service>()
                .Query()
                .Include(s => s.Category)
                .FirstAsync(s => s.Id == service.Id);

            return ServiceResult<ServiceDto>.SuccessResult(MapToServiceDto(updated), "Service updated successfully");
        }
        catch (Exception ex)
        {
            return ServiceResult<ServiceDto>.FailureResult($"Error updating service: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteServiceAsync(Guid serviceId)
    {
        try
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(serviceId);
            if (service == null)
                return ServiceResult<bool>.FailureResult("Service not found");

            if (!service.IsActive)
                return ServiceResult<bool>.FailureResult("Service is already inactive");

            service.IsActive = false;
            await _unitOfWork.Repository<Service>().UpdateAsync(service);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<bool>.SuccessResult(true, "Service deleted successfully");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error deleting service: {ex.Message}");
        }
    }

    public async Task<ServiceResult<AppointmentResponseDto>> CreateAppointmentAsync(CreateAppointmentDto dto, Guid userId)
    {
        try
        {
            if (dto.AppointmentType.Contains("home", StringComparison.OrdinalIgnoreCase)
                || dto.AppointmentType.Contains("tại nhà", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<AppointmentResponseDto>.FailureResult("Dịch vụ tại nhà đã bị tắt. Chỉ hỗ trợ dịch vụ tại trung tâm.");
            }

            Service? service = null;
            if (dto.ServiceId.HasValue)
            {
                service = await _unitOfWork.Services.GetByIdAsync(dto.ServiceId.Value);
                if (service == null)
                    return ServiceResult<AppointmentResponseDto>.FailureResult("Service not found");

                if (service.IsHomeService)
                    return ServiceResult<AppointmentResponseDto>.FailureResult("Dịch vụ tại nhà đã bị tắt. Vui lòng đặt lịch tại trung tâm.");
            }

            var appointment = new Appointment
            {
                UserId = userId,
                Pet = string.IsNullOrWhiteSpace(dto.Pet) ? null : dto.Pet.Trim(),
                ServiceId = dto.ServiceId,
                AppointmentType = dto.AppointmentType,
                AppointmentStatus = "pending",
                BranchId = dto.BranchId,
                AppointmentDate = DateTime.SpecifyKind(dto.AppointmentDate, DateTimeKind.Utc),
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                ServiceAddress = null,
                CheckInCode = await GenerateCheckInCodeAsync(dto.AppointmentDate),
                Notes = dto.Notes
            };

            await _unitOfWork.Repository<Appointment>().AddAsync(appointment);

            // Record initial status history
            var history = new AppointmentStatusHistory
            {
                AppointmentId = appointment.Id,
                Status = "pending",
                Notes = $"Lịch hẹn được tạo. Mã check-in: {appointment.CheckInCode}",
                UpdatedBy = userId
            };
            await _unitOfWork.Repository<AppointmentStatusHistory>().AddAsync(history);

            await _unitOfWork.SaveChangesAsync();
            try
            {
                var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
                if (user != null)
                {
                    await _emailService.SendEmailAsync(
                        user.Email,
                        "Đặt lịch hẹn thành công - PetCare",
                        BuildBookingConfirmationEmailBody(user.FullName, appointment.AppointmentDate, appointment.StartTime, service?.ServiceName)
                    );
                }
            }
            catch
            {
                // Bỏ qua lỗi email
            }

            var created = await _unitOfWork.Appointments.GetAppointmentWithDetailsAsync(appointment.Id);
            return ServiceResult<AppointmentResponseDto>.SuccessResult(MapToResponseDto(created!), $"Đặt lịch hẹn thành công. Mã check-in của bạn: {appointment.CheckInCode}");
        }
        catch (Exception ex)
        {
            return ServiceResult<AppointmentResponseDto>.FailureResult($"Error creating appointment: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<AppointmentResponseDto>>> GetUserAppointmentsAsync(Guid userId)
    {
        try
        {
            var appointments = await _unitOfWork.Appointments.GetAppointmentsByUserIdAsync(userId);
            var dtos = appointments.Select(MapToResponseDto);
            return ServiceResult<IEnumerable<AppointmentResponseDto>>.SuccessResult(dtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<AppointmentResponseDto>>.FailureResult($"Error retrieving appointments: {ex.Message}");
        }
    }

    public async Task<ServiceResult<AppointmentResponseDto>> GetAppointmentByIdAsync(Guid appointmentId, Guid userId, string userRole)
    {
        try
        {
            var appointment = await _unitOfWork.Appointments.GetAppointmentWithDetailsAsync(appointmentId);
            if (appointment == null)
                return ServiceResult<AppointmentResponseDto>.FailureResult("Appointment not found");

            bool isPrivileged = IsStaffOrAdmin(userRole);

            if (!isPrivileged && appointment.UserId != userId)
                return ServiceResult<AppointmentResponseDto>.FailureResult("You do not have permission to view this appointment");

            return ServiceResult<AppointmentResponseDto>.SuccessResult(MapToResponseDto(appointment));
        }
        catch (Exception ex)
        {
            return ServiceResult<AppointmentResponseDto>.FailureResult($"Error retrieving appointment: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> CancelAppointmentAsync(Guid appointmentId, Guid userId, string? cancellationReason)
    {
        try
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId);
            if (appointment == null)
                return ServiceResult<bool>.FailureResult("Appointment not found");

            if (appointment.UserId != userId)
                return ServiceResult<bool>.FailureResult("You do not have permission to cancel this appointment");

            if (appointment.AppointmentStatus == "completed")
                return ServiceResult<bool>.FailureResult("Cannot cancel a completed appointment");

            if (appointment.AppointmentStatus == "cancelled")
                return ServiceResult<bool>.FailureResult("Appointment is already cancelled");

            // Fix DateTime về UTC
            appointment.AppointmentDate = DateTime.SpecifyKind(appointment.AppointmentDate, DateTimeKind.Utc);
            appointment.CreatedAt = DateTime.SpecifyKind(appointment.CreatedAt, DateTimeKind.Utc);
            appointment.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            appointment.AppointmentStatus = "cancelled";
            appointment.CancellationReason = cancellationReason;
            await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);

            var history = new AppointmentStatusHistory
            {
                AppointmentId = appointment.Id,
                Status = "cancelled",
                Notes = cancellationReason ?? "Khách hàng huỷ lịch",
                UpdatedBy = userId,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            };
            await _unitOfWork.Repository<AppointmentStatusHistory>().AddAsync(history);

            await _unitOfWork.SaveChangesAsync();

            // Gửi email thông báo huỷ
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(appointment.UserId);
                if (user != null)
                {
                    await _emailService.SendEmailAsync(
                         user.Email,
                         "Xác nhận huỷ lịch hẹn - PetCare",
                         BuildCancelledEmailBody(user.FullName, appointment.AppointmentDate, cancellationReason)
                     );
                }
            }
            catch
            {
                // Bỏ qua lỗi email, không ảnh hưởng kết quả chính
            }

            return ServiceResult<bool>.SuccessResult(true, "Lịch hẹn đã được huỷ");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error cancelling appointment: {ex.Message} | Inner: {ex.InnerException?.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<AppointmentResponseDto>>> GetAllAppointmentsAsync(string? status, DateTime? date)
    {
        try
        {
            var appointments = await _unitOfWork.Appointments.GetAllWithDetailsAsync(status, date);
            var dtos = appointments.Select(MapToResponseDto);
            return ServiceResult<IEnumerable<AppointmentResponseDto>>.SuccessResult(dtos);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<AppointmentResponseDto>>.FailureResult($"Error retrieving appointments: {ex.Message}");
        }
    }

    public async Task<ServiceResult<AppointmentResponseDto>> UpdateAppointmentStatusAsync(Guid appointmentId, UpdateAppointmentStatusDto dto, Guid staffId)
    {
        try
        {
            var validStatuses = new[] { "pending", "confirmed", "checked-in", "in-progress", "completed", "cancelled" };
            if (!validStatuses.Contains(dto.Status))
                return ServiceResult<AppointmentResponseDto>.FailureResult($"Invalid status. Valid values: {string.Join(", ", validStatuses)}");

            var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId);
            if (appointment == null)
                return ServiceResult<AppointmentResponseDto>.FailureResult("Appointment not found");

            // Fix DateTime về UTC
            appointment.AppointmentDate = DateTime.SpecifyKind(appointment.AppointmentDate, DateTimeKind.Utc);
            appointment.CreatedAt = DateTime.SpecifyKind(appointment.CreatedAt, DateTimeKind.Utc);
            appointment.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            appointment.AppointmentStatus = dto.Status;
            appointment.AssignedStaffId = staffId;

            if (dto.Status == "checked-in")
                appointment.CheckedInAt = DateTime.UtcNow;

            if (dto.Status == "in-progress")
                appointment.StartedAt = DateTime.UtcNow;

            if (dto.Status == "completed")
            {
                appointment.CompletedAt = DateTime.UtcNow;
                appointment.BillNumber ??= GenerateBillNumber(appointment.Id);
            }

            if (!string.IsNullOrWhiteSpace(dto.MedicalNotes))
                appointment.Notes = dto.MedicalNotes;

            if (dto.Status == "cancelled" && !string.IsNullOrWhiteSpace(dto.CancellationReason))
                appointment.CancellationReason = dto.CancellationReason;

            await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);

            var history = new AppointmentStatusHistory
            {
                AppointmentId = appointment.Id,
                Status = dto.Status,
                Notes = dto.MedicalNotes ?? dto.CancellationReason,
                UpdatedBy = staffId,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            };
            await _unitOfWork.Repository<AppointmentStatusHistory>().AddAsync(history);

            await _unitOfWork.SaveChangesAsync();

            // Gửi email thông báo
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(appointment.UserId);
                if (user != null)
                {
                    if (dto.Status == "confirmed")
                    {
                        await _emailService.SendEmailAsync(
                            user.Email,
                            "Lịch hẹn đã được xác nhận - PetCare",
                            BuildConfirmedEmailBody(user.FullName, appointment.AppointmentDate, appointment.StartTime, dto.MedicalNotes)
                        );
                    }
                    else if (dto.Status == "cancelled")
                    {
                        await _emailService.SendEmailAsync(
                            user.Email,
                            "Lịch hẹn đã bị huỷ - PetCare",
                            BuildCancelledEmailBody(user.FullName, appointment.AppointmentDate, dto.CancellationReason)
                        );
                    }
                    else if (dto.Status == "completed")
                    {
                        await _emailService.SendEmailAsync(
                            user.Email,
                            "Dịch vụ đã hoàn thành - PetCare",
                            BuildCompletedEmailBody(user.FullName, appointment.AppointmentDate, dto.MedicalNotes)
                        );
                    }

                }
            }
            catch
            {
                // Bỏ qua lỗi email, không ảnh hưởng kết quả chính
            }

            var updated = await _unitOfWork.Appointments.GetAppointmentWithDetailsAsync(appointment.Id);
            return ServiceResult<AppointmentResponseDto>.SuccessResult(MapToResponseDto(updated!), "Cập nhật trạng thái thành công");
        }
        catch (Exception ex)
        {
            return ServiceResult<AppointmentResponseDto>.FailureResult($"Error: {ex.Message} | Inner: {ex.InnerException?.Message}");
        }
    }

    public Task<ServiceResult<AppointmentResponseDto>> ConfirmAppointmentAsync(Guid appointmentId, Guid staffId, string? notes)
    {
        return UpdateAppointmentStatusAsync(appointmentId, new UpdateAppointmentStatusDto
        {
            Status = "confirmed",
            MedicalNotes = notes
        }, staffId);
    }

    public async Task<ServiceResult<AppointmentResponseDto>> CheckInAppointmentAsync(Guid appointmentId, string checkInCode, Guid staffId)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId);
        if (appointment == null)
            return ServiceResult<AppointmentResponseDto>.FailureResult("Appointment not found");

        if (appointment.AppointmentStatus != "confirmed")
            return ServiceResult<AppointmentResponseDto>.FailureResult("Chỉ có thể check-in khi lịch đã được xác nhận");

        if (string.IsNullOrWhiteSpace(appointment.CheckInCode)
            || !string.Equals(appointment.CheckInCode, checkInCode?.Trim(), StringComparison.Ordinal))
        {
            return ServiceResult<AppointmentResponseDto>.FailureResult("Mã check-in không hợp lệ");
        }

        appointment.AppointmentStatus = "checked-in";
        appointment.AssignedStaffId = staffId;
        appointment.CheckedInAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);

        await _unitOfWork.Repository<AppointmentStatusHistory>().AddAsync(new AppointmentStatusHistory
        {
            AppointmentId = appointment.Id,
            Status = "checked-in",
            Notes = "Khách đã check-in tại quầy",
            UpdatedBy = staffId,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();

        var updated = await _unitOfWork.Appointments.GetAppointmentWithDetailsAsync(appointmentId);
        return ServiceResult<AppointmentResponseDto>.SuccessResult(MapToResponseDto(updated!), "Check-in thành công");
    }

    public async Task<ServiceResult<AppointmentResponseDto>> StartServiceAsync(Guid appointmentId, StartAppointmentServiceDto dto, Guid staffId)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId);
        if (appointment == null)
            return ServiceResult<AppointmentResponseDto>.FailureResult("Appointment not found");

        if (appointment.AppointmentStatus != "checked-in" && appointment.AppointmentStatus != "confirmed")
            return ServiceResult<AppointmentResponseDto>.FailureResult("Lịch chưa ở trạng thái check-in/confirmed");

        var selected = dto.Services
            .Where(s => s.ServiceId != Guid.Empty && s.Quantity > 0)
            .ToList();
        if (selected.Count == 0)
            return ServiceResult<AppointmentResponseDto>.FailureResult("Vui lòng chọn ít nhất 1 dịch vụ");

        var existingItems = await _unitOfWork.Repository<AppointmentServiceItem>()
            .FindAsync(i => i.AppointmentId == appointmentId);
        var existingList = existingItems.ToList();
        if (existingList.Count > 0)
            await _unitOfWork.Repository<AppointmentServiceItem>().DeleteRangeAsync(existingList);

        decimal total = 0m;
        var itemsToAdd = new List<AppointmentServiceItem>();
        foreach (var selectedService in selected)
        {
            var service = await _unitOfWork.Services.GetByIdAsync(selectedService.ServiceId);
            if (service == null || !service.IsActive)
                return ServiceResult<AppointmentResponseDto>.FailureResult("Một trong các dịch vụ không tồn tại hoặc đã ngừng hoạt động");

            if (service.IsHomeService)
                return ServiceResult<AppointmentResponseDto>.FailureResult("Không hỗ trợ dịch vụ tại nhà");

            var lineTotal = service.Price * selectedService.Quantity;
            total += lineTotal;

            itemsToAdd.Add(new AppointmentServiceItem
            {
                AppointmentId = appointmentId,
                ServiceId = service.Id,
                Quantity = selectedService.Quantity,
                UnitPrice = service.Price,
                LineTotal = lineTotal
            });
        }

        await _unitOfWork.Repository<AppointmentServiceItem>().AddRangeAsync(itemsToAdd);

        appointment.AssignedStaffId = staffId;
        appointment.AppointmentStatus = "in-progress";
        appointment.StartedAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;
        appointment.TotalAmount = total;
        appointment.ServiceId = itemsToAdd.First().ServiceId;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
            appointment.Notes = dto.Notes;

        await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);

        var selectedSummary = string.Join(", ", itemsToAdd.Select(i => $"{i.Quantity}x {i.ServiceId}"));
        await _unitOfWork.Repository<AppointmentStatusHistory>().AddAsync(new AppointmentStatusHistory
        {
            AppointmentId = appointment.Id,
            Status = "in-progress",
            Notes = string.IsNullOrWhiteSpace(dto.Notes)
                ? $"Bắt đầu thực hiện dịch vụ. Danh mục đã chọn: {selectedSummary}"
                : $"{dto.Notes}. Danh mục đã chọn: {selectedSummary}",
            UpdatedBy = staffId,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();

        var updated = await _unitOfWork.Appointments.GetAppointmentWithDetailsAsync(appointmentId);
        return ServiceResult<AppointmentResponseDto>.SuccessResult(MapToResponseDto(updated!), "Đã chuyển sang trạng thái đang thực hiện");
    }

    public async Task<ServiceResult<AppointmentResponseDto>> CompleteAppointmentAsync(Guid appointmentId, Guid staffId, string? notes)
    {
        var appointment = await _unitOfWork.Appointments.GetAppointmentWithDetailsAsync(appointmentId);
        if (appointment == null)
            return ServiceResult<AppointmentResponseDto>.FailureResult("Appointment not found");

        if (appointment.AppointmentStatus != "in-progress")
            return ServiceResult<AppointmentResponseDto>.FailureResult("Chỉ có thể hoàn thành khi lịch ở trạng thái in-progress");

        if (!appointment.AppointmentServiceItems.Any() && appointment.ServiceId.HasValue)
        {
            var fallback = await _unitOfWork.Services.GetByIdAsync(appointment.ServiceId.Value);
            if (fallback != null)
            {
                var item = new AppointmentServiceItem
                {
                    AppointmentId = appointment.Id,
                    ServiceId = fallback.Id,
                    Quantity = 1,
                    UnitPrice = fallback.Price,
                    LineTotal = fallback.Price
                };

                await _unitOfWork.Repository<AppointmentServiceItem>().AddAsync(item);
                appointment.TotalAmount = fallback.Price;
            }
        }

        appointment.AppointmentStatus = "completed";
        appointment.AssignedStaffId = staffId;
        appointment.CompletedAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;
        appointment.BillNumber ??= GenerateBillNumber(appointment.Id);

        if (!string.IsNullOrWhiteSpace(notes))
            appointment.Notes = notes;

        if (!appointment.TotalAmount.HasValue)
            appointment.TotalAmount = appointment.AppointmentServiceItems.Sum(i => i.LineTotal);

        await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);

        await _unitOfWork.Repository<AppointmentStatusHistory>().AddAsync(new AppointmentStatusHistory
        {
            AppointmentId = appointment.Id,
            Status = "completed",
            Notes = notes ?? $"Hoàn thành dịch vụ. Bill: {appointment.BillNumber}",
            UpdatedBy = staffId,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();

        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(appointment.UserId);
            if (user != null)
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Dịch vụ đã hoàn thành - PetCare",
                    BuildCompletedEmailBody(user.FullName, appointment.AppointmentDate, notes)
                );
            }
        }
        catch
        {
            // Ignore email errors to avoid blocking main flow.
        }

        var updated = await _unitOfWork.Appointments.GetAppointmentWithDetailsAsync(appointmentId);
        return ServiceResult<AppointmentResponseDto>.SuccessResult(MapToResponseDto(updated!), "Hoàn thành dịch vụ thành công");
    }

    public async Task<ServiceResult<AppointmentBillDto>> GetAppointmentBillAsync(Guid appointmentId, Guid userId, string userRole)
    {
        var appointment = await _unitOfWork.Appointments.GetAppointmentWithDetailsAsync(appointmentId);
        if (appointment == null)
            return ServiceResult<AppointmentBillDto>.FailureResult("Appointment not found");

        var isPrivileged = IsStaffOrAdmin(userRole);
        if (!isPrivileged && appointment.UserId != userId)
            return ServiceResult<AppointmentBillDto>.FailureResult("You do not have permission to view this bill");

        var items = appointment.AppointmentServiceItems
            .Select(i => new AppointmentServiceItemResponseDto
            {
                ServiceId = i.ServiceId,
                ServiceName = i.Service?.ServiceName ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            })
            .ToList();

        if (items.Count == 0 && appointment.Service != null)
        {
            items.Add(new AppointmentServiceItemResponseDto
            {
                ServiceId = appointment.Service.Id,
                ServiceName = appointment.Service.ServiceName,
                Quantity = 1,
                UnitPrice = appointment.Service.Price,
                LineTotal = appointment.Service.Price
            });
        }

        var total = appointment.TotalAmount ?? items.Sum(i => i.LineTotal);

        var bill = new AppointmentBillDto
        {
            AppointmentId = appointment.Id,
            BillNumber = appointment.BillNumber ?? GenerateBillNumber(appointment.Id),
            BillDate = appointment.CompletedAt ?? appointment.UpdatedAt ?? DateTime.UtcNow,
            CustomerName = appointment.User?.FullName ?? string.Empty,
            Pet = appointment.Pet,
            BranchName = appointment.Branch?.BranchName ?? "PetCare Center",
            Items = items,
            TotalAmount = total
        };

        return ServiceResult<AppointmentBillDto>.SuccessResult(bill);
    }


    private static AppointmentResponseDto MapToResponseDto(Appointment a) => new()
    {
        Id = a.Id,
        UserId = a.UserId,
        UserName = a.User?.FullName ?? string.Empty,
        Pet = a.Pet,
        ServiceId = a.ServiceId,
        ServiceName = a.Service?.ServiceName,
        ServicePrice = a.Service?.Price,
        AppointmentType = a.AppointmentType,
        AppointmentStatus = a.AppointmentStatus,
        BranchId = a.BranchId,
        BranchName = a.Branch?.BranchName,
        AssignedStaffId = a.AssignedStaffId,
        AssignedStaffName = a.AssignedStaff?.FullName,
        AppointmentDate = a.AppointmentDate,
        StartTime = a.StartTime,
        EndTime = a.EndTime,
        Notes = a.Notes,
        CancellationReason = a.CancellationReason,
        CheckInCode = a.CheckInCode,
        CheckedInAt = a.CheckedInAt,
        StartedAt = a.StartedAt,
        CompletedAt = a.CompletedAt,
        BillNumber = a.BillNumber,
        TotalAmount = a.TotalAmount,
        SelectedServices = a.AppointmentServiceItems
            .OrderBy(i => i.CreatedAt)
            .Select(i => new AppointmentServiceItemResponseDto
            {
                ServiceId = i.ServiceId,
                ServiceName = i.Service?.ServiceName ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            })
            .ToList(),
        StatusHistory = a.StatusHistory
            .OrderBy(h => h.CreatedAt)
            .Select(h => new AppointmentStatusHistoryDto
            {
                Status = h.Status,
                Notes = h.Notes,
                CreatedAt = h.CreatedAt
            })
            .ToList(),
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };

    private static ServiceDto MapToServiceDto(Service s) => new()
    {
        Id = s.Id,
        CategoryId = s.CategoryId,
        ServiceName = s.ServiceName,
        Description = s.Description,
        DurationMinutes = s.DurationMinutes,
        Price = s.Price,
        IsHomeService = s.IsHomeService,
        IsActive = s.IsActive,
        CategoryName = s.Category?.CategoryName,
        CreatedAt = s.CreatedAt
    };

    private string BuildConfirmedEmailBody(string fullName, DateTime appointmentDate, TimeSpan? startTime, string? notes) => $"""
    <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
        <div style="background-color: #4f9d69; padding: 30px; text-align: center; border-radius: 8px 8px 0 0;">
            <h1 style="color: white; margin: 0;">Lịch hẹn đã được xác nhận ✅</h1>
        </div>
        <div style="padding: 30px; background-color: #f9f9f9; border-radius: 0 0 8px 8px;">
            <p style="font-size: 16px;">Xin chào <strong>{fullName}</strong>,</p>
            <p style="font-size: 16px;">Lịch hẹn của bạn đã được <strong>xác nhận</strong> thành công.</p>
            <div style="background-color: #fff; border: 1px solid #e0e0e0; border-radius: 6px; padding: 20px; margin: 20px 0;">
                <p style="margin: 0; font-size: 15px;"><strong>Ngày hẹn:</strong> {appointmentDate:dd/MM/yyyy}</p>
                <p style="margin: 8px 0 0; font-size: 15px;"><strong>Giờ bắt đầu:</strong> {startTime}</p>
                {(!string.IsNullOrWhiteSpace(notes) ? $"<p style=\"margin: 8px 0 0; font-size: 15px;\"><strong>Ghi chú:</strong> {notes}</p>" : "")}
            </div>
            <div style="text-align: center; margin: 30px 0;">
                <a href="https://pettsuba.live" style="background-color: #4f9d69; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-size: 16px;">Xem lịch hẹn</a>
            </div>
            <p style="color: #888; font-size: 13px; text-align: center;">Cảm ơn bạn đã sử dụng dịch vụ PetCare! 🐾</p>
        </div>
    </div>
    """;

    private string BuildCancelledEmailBody(string fullName, DateTime appointmentDate, string? reason) => $"""
    <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
        <div style="background-color: #dc2626; padding: 30px; text-align: center; border-radius: 8px 8px 0 0;">
            <h1 style="color: white; margin: 0;">Lịch hẹn đã bị huỷ ❌</h1>
        </div>
        <div style="padding: 30px; background-color: #f9f9f9; border-radius: 0 0 8px 8px;">
            <p style="font-size: 16px;">Xin chào <strong>{fullName}</strong>,</p>
            <p style="font-size: 16px;">Lịch hẹn của bạn đã bị <strong>huỷ</strong>.</p>
            <div style="background-color: #fff; border: 1px solid #e0e0e0; border-radius: 6px; padding: 20px; margin: 20px 0;">
                <p style="margin: 0; font-size: 15px;"><strong>Ngày hẹn:</strong> {appointmentDate:dd/MM/yyyy}</p>
                <p style="margin: 8px 0 0; font-size: 15px;"><strong>Lý do:</strong> {reason ?? "Không có lý do"}</p>
            </div>
            <div style="text-align: center; margin: 30px 0;">
                <a href="https://pettsuba.live" style="background-color: #4f9d69; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-size: 16px;">Đặt lịch lại</a>
            </div>
            <p style="color: #888; font-size: 13px; text-align: center;">Vui lòng liên hệ PetCare nếu bạn có thắc mắc.</p>
        </div>
    </div>
    """;

    private string BuildCompletedEmailBody(string fullName, DateTime appointmentDate, string? notes) => $"""
    <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
        <div style="background-color: #2563eb; padding: 30px; text-align: center; border-radius: 8px 8px 0 0;">
            <h1 style="color: white; margin: 0;">Lịch hẹn đã hoàn thành 🎉</h1>
        </div>
        <div style="padding: 30px; background-color: #f9f9f9; border-radius: 0 0 8px 8px;">
            <p style="font-size: 16px;">Xin chào <strong>{fullName}</strong>,</p>
            <p style="font-size: 16px;">Lịch hẹn của bạn đã <strong>hoàn thành</strong> thành công.</p>
            <div style="background-color: #fff; border: 1px solid #e0e0e0; border-radius: 6px; padding: 20px; margin: 20px 0;">
                <p style="margin: 0; font-size: 15px;"><strong>Ngày hẹn:</strong> {appointmentDate:dd/MM/yyyy}</p>
                {(!string.IsNullOrWhiteSpace(notes) ? $"<p style=\"margin: 8px 0 0; font-size: 15px;\"><strong>Ghi chú nhân viên:</strong> {notes}</p>" : "")}
            </div>
            <div style="text-align: center; margin: 30px 0;">
                <a href="https://pettsuba.live" style="background-color: #2563eb; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-size: 16px;">Xem chi tiết</a>
            </div>
            <p style="color: #888; font-size: 13px; text-align: center;">Cảm ơn bạn đã sử dụng dịch vụ PetCare! 🐾</p>
        </div>
    </div>
    """;
    private string BuildBookingConfirmationEmailBody(string fullName, DateTime appointmentDate, TimeSpan? startTime, string? serviceName) => $"""
    <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
        <div style="background-color: #4f9d69; padding: 30px; text-align: center; border-radius: 8px 8px 0 0;">
            <h1 style="color: white; margin: 0;">Đặt lịch hẹn thành công 🐾</h1>
        </div>
        <div style="padding: 30px; background-color: #f9f9f9; border-radius: 0 0 8px 8px;">
            <p style="font-size: 16px;">Xin chào <strong>{fullName}</strong>,</p>
            <p style="font-size: 16px;">Bạn đã đặt lịch thành công tại trung tâm. Nhân viên sẽ xác nhận lịch sớm nhất có thể.</p>
            <div style="background-color: #fff; border: 1px solid #e0e0e0; border-radius: 6px; padding: 20px; margin: 20px 0;">
                <p style="margin: 0; font-size: 15px;"><strong>Dịch vụ:</strong> {serviceName ?? "N/A"}</p>
                <p style="margin: 8px 0 0; font-size: 15px;"><strong>Ngày hẹn:</strong> {appointmentDate:dd/MM/yyyy}</p>
                <p style="margin: 8px 0 0; font-size: 15px;"><strong>Giờ bắt đầu:</strong> {startTime}</p>
                <p style="margin: 8px 0 0; font-size: 15px;"><strong>Trạng thái:</strong> Chờ xác nhận</p>
            </div>
            <div style="text-align: center; margin: 30px 0;">
                <a href="https://pettsuba.live" style="background-color: #4f9d69; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-size: 16px;">Xem lịch hẹn</a>
            </div>
            <p style="color: #888; font-size: 13px; text-align: center;">Cảm ơn bạn đã sử dụng dịch vụ PetCare! 🐾</p>
        </div>
    </div>
    """;

    private static bool IsStaffOrAdmin(string role)
    {
        return role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
               || role.Equals("Staff", StringComparison.OrdinalIgnoreCase)
               || role.Equals("Provider", StringComparison.OrdinalIgnoreCase)
               || role.Equals("ServiceProvider", StringComparison.OrdinalIgnoreCase)
               || role.Equals("service_provider", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> GenerateCheckInCodeAsync(DateTime appointmentDate)
    {
        var targetDate = appointmentDate.Date;
        for (var i = 0; i < 20; i++)
        {
            var code = RandomNumberGenerator.GetInt32(10000, 100000).ToString();
            var exists = await _unitOfWork.Repository<Appointment>()
                .AnyAsync(a => a.AppointmentDate.Date == targetDate && a.CheckInCode == code);
            if (!exists)
                return code;
        }

        return RandomNumberGenerator.GetInt32(10000, 100000).ToString();
    }

    private static string GenerateBillNumber(Guid appointmentId)
    {
        return $"SV-{DateTime.UtcNow:yyyyMMdd}-{appointmentId.ToString("N")[..6].ToUpperInvariant()}";
    }
}