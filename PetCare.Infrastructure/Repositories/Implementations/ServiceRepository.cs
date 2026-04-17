using Microsoft.EntityFrameworkCore;
using PetCare.Domain.Entities;
using PetCare.Infrastructure.Data;
using PetCare.Infrastructure.Repositories.Interfaces;

namespace PetCare.Infrastructure.Repositories.Implementations;

public class ServiceRepository : GenericRepository<Service>, IServiceRepository
{
    public ServiceRepository(PetCareDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Service>> GetActiveServicesAsync()
    {
        return await _dbSet
            .Where(s => s.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Service>> GetServicesByCategoryAsync(Guid categoryId)
    {
        return await _dbSet
            .Where(s => s.IsActive)
            .ToListAsync();
    }

    public async Task<Service?> GetServiceWithDetailsAsync(Guid serviceId)
    {
        return await _dbSet
            .Include(s => s.StaffServices)
                .ThenInclude(ss => ss.User)
            .FirstOrDefaultAsync(s => s.Id == serviceId);
    }
}
