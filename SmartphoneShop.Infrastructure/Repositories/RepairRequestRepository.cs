using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Infrastructure.Data;

namespace SmartphoneShop.Infrastructure.Repositories;

public class RepairRequestRepository : GenericRepository<RepairRequest>, IRepairRequestRepository
{
    public RepairRequestRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<RepairRequest>> GetByUserIdAsync(string userId)
    {
        return await _dbSet
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<RepairRequest>> GetAllAsync()
    {
        return await _dbSet
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
}