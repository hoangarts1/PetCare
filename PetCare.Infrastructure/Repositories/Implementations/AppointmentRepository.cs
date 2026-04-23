using Microsoft.EntityFrameworkCore;
using PetCare.Domain.Entities;
using PetCare.Infrastructure.Data;
using PetCare.Infrastructure.Repositories.Interfaces;

namespace PetCare.Infrastructure.Repositories.Implementations;

public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(PetCareDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(a => a.Service)
            .Include(a => a.AssignedStaff)
            .Include(a => a.StatusHistory)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByDateAsync(DateTime date)
    {
        return await _dbSet
            .Include(a => a.User)
            .Include(a => a.Service)
            .Include(a => a.AssignedStaff)
            .Where(a => a.AppointmentDate.Date == date.Date)
            .OrderBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<Appointment?> GetAppointmentWithDetailsAsync(Guid appointmentId)
    {
        return await _dbSet
            .Include(a => a.User)
            .Include(a => a.Service)
            .Include(a => a.AssignedStaff)
            .Include(a => a.StatusHistory)
            .Include(a => a.UsedServices)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByStaffAsync(Guid staffId, DateTime date)
    {
        return await _dbSet
            .Include(a => a.User)
            .Include(a => a.Service)
            .Where(a => a.AssignedStaffId == staffId && a.AppointmentDate.Date == date.Date)
            .OrderBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetAllWithDetailsAsync(string? status, DateTime? date)
    {
        var query = _dbSet
            .Include(a => a.User)
            .Include(a => a.Service)
            .Include(a => a.AssignedStaff)
            .Include(a => a.StatusHistory)
            .Include(a => a.UsedServices)
            .AsQueryable();

        var normalizedStatus = NormalizeStatusFilter(status);
        if (!string.IsNullOrEmpty(normalizedStatus))
        {
            query = query.Where(a => (a.AppointmentStatus ?? string.Empty).ToLower() == normalizedStatus);
        }

        if (date.HasValue)
        {
            // Use UTC date range to avoid DateTimeKind issues with PostgreSQL timestamptz.
            var normalizedDateUtc = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
            var nextDateUtc = normalizedDateUtc.AddDays(1);
            query = query.Where(a => a.AppointmentDate >= normalizedDateUtc && a.AppointmentDate < nextDateUtc);
        }

        return await query.OrderByDescending(a => a.AppointmentDate).ToListAsync();
    }

    private static string? NormalizeStatusFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        var normalized = status.Trim().ToLowerInvariant().Replace("_", "-").Replace(" ", "-");
        return normalized switch
        {
            "all" => null,
            "used" => "completed",
            "done" => "completed",
            "checkedin" => "checked-in",
            "checkin" => "checked-in",
            "inprogress" => "in-progress",
            "canceled" => "cancelled",
            _ => normalized
        };
    }
}
