using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCare.Application.DTOs.Appointment;
using PetCare.Application.DTOs.Service;
using PetCare.Application.Services.Interfaces;

namespace PetCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private const string StaffAdminRoles = "Staff,staff,Admin,admin,Provider,provider,ServiceProvider,serviceprovider,Service_Provider,service_provider";
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    /// <summary>
    /// Get all active services available for booking (public)
    /// </summary>
    [HttpGet("services")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailableServices()
    {
        var result = await _appointmentService.GetAvailableServicesAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Staff / Admin gets all services (including inactive)
    /// </summary>
    [HttpGet("services/all")]
    [Authorize(Roles = StaffAdminRoles)]
    public async Task<IActionResult> GetAllServices()
    {
        var result = await _appointmentService.GetAllServicesAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Staff / Admin gets service detail by ID
    /// </summary>
    [HttpGet("services/{id:guid}")]
    [Authorize(Roles = StaffAdminRoles)]
    public async Task<IActionResult> GetServiceById(Guid id)
    {
        var result = await _appointmentService.GetServiceByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Staff / Admin creates a service
    /// </summary>
    [HttpPost("services")]
    [Authorize(Roles = StaffAdminRoles)]
    public async Task<IActionResult> CreateService([FromBody] CreateServiceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _appointmentService.CreateServiceAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Staff / Admin updates a service
    /// </summary>
    [HttpPut("services/{id:guid}")]
    [Authorize(Roles = StaffAdminRoles)]
    public async Task<IActionResult> UpdateService(Guid id, [FromBody] UpdateServiceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _appointmentService.UpdateServiceAsync(id, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Staff / Admin soft-deletes a service
    /// </summary>
    [HttpDelete("services/{id:guid}")]
    [Authorize(Roles = StaffAdminRoles)]
    public async Task<IActionResult> DeleteService(Guid id)
    {
        var result = await _appointmentService.DeleteServiceAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Customer creates a new appointment booking
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        var result = await _appointmentService.CreateAppointmentAsync(dto, userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Customer views their own appointments
    /// </summary>
    [HttpGet("my-appointments")]
    [Authorize]
    public async Task<IActionResult> GetMyAppointments()
    {
        var userId = GetUserId();
        var result = await _appointmentService.GetUserAppointmentsAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get a single appointment by ID (customer sees own; staff/admin see any)
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetAppointmentById(Guid id)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();
        var result = await _appointmentService.GetAppointmentByIdAsync(id, userId, userRole);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Customer cancels their appointment
    /// </summary>
    [HttpPatch("{id:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelAppointment(Guid id, [FromBody] CancelAppointmentRequest? request)
    {
        var userId = GetUserId();
        var result = await _appointmentService.CancelAppointmentAsync(id, userId, request?.CancellationReason);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Staff / Admin views all appointments with optional status and date filters
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Staff,staff,Admin,admin,Provider,provider,ServiceProvider,serviceprovider,Service_Provider,service_provider")]
    public async Task<IActionResult> GetAllAppointments([FromQuery] string? status, [FromQuery] DateTime? date)
    {
        var result = await _appointmentService.GetAllAppointmentsAsync(status, date);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Staff / Admin updates appointment status
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Staff,staff,Admin,admin,Provider,provider,ServiceProvider,serviceprovider,Service_Provider,service_provider")]
    public async Task<IActionResult> UpdateAppointmentStatus(Guid id, [FromBody] UpdateAppointmentStatusDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var staffId = GetUserId();
        var result = await _appointmentService.UpdateAppointmentStatusAsync(id, dto, staffId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id:guid}/confirm")]
    [Authorize(Roles = "Staff,staff,Admin,admin,Provider,provider,ServiceProvider,serviceprovider,Service_Provider,service_provider")]
    public async Task<IActionResult> ConfirmAppointment(Guid id, [FromBody] ConfirmAppointmentDto dto)
    {
        var staffId = GetUserId();
        var result = await _appointmentService.ConfirmAppointmentAsync(id, staffId, dto.Notes);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id:guid}/check-in")]
    [Authorize(Roles = "Staff,staff,Admin,admin,Provider,provider,ServiceProvider,serviceprovider,Service_Provider,service_provider")]
    public async Task<IActionResult> CheckInAppointment(Guid id, [FromBody] CheckInAppointmentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var staffId = GetUserId();
        var result = await _appointmentService.CheckInAppointmentAsync(id, dto.CheckInCode, staffId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id:guid}/start-service")]
    [Authorize(Roles = "Staff,staff,Admin,admin,Provider,provider,ServiceProvider,serviceprovider,Service_Provider,service_provider")]
    public async Task<IActionResult> StartService(Guid id, [FromBody] StartAppointmentServiceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var staffId = GetUserId();
        var result = await _appointmentService.StartServiceAsync(id, dto, staffId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id:guid}/complete")]
    [Authorize(Roles = "Staff,staff,Admin,admin,Provider,provider,ServiceProvider,serviceprovider,Service_Provider,service_provider")]
    public async Task<IActionResult> CompleteService(Guid id, [FromBody] CompleteAppointmentDto dto)
    {
        var staffId = GetUserId();
        var result = await _appointmentService.CompleteAppointmentAsync(id, staffId, dto.Notes);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}/bill")]
    [Authorize]
    public async Task<IActionResult> GetBill(Guid id)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();
        var result = await _appointmentService.GetAppointmentBillAsync(id, userId, userRole);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(idClaim, out var id)) return id;
        throw new UnauthorizedAccessException("Invalid user identity");
    }

    private string GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}

public class CancelAppointmentRequest
{
    public string? CancellationReason { get; set; }
}
