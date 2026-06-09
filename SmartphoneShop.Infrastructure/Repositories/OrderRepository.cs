using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Infrastructure.Data;
using X.PagedList;

namespace SmartphoneShop.Infrastructure.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId)
    {
        return await _dbSet
            .Include(o => o.Items)
            .ThenInclude(i => i.Smartphone)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IPagedList<Order>> GetByUserIdPagedAsync(string userId, int pageNumber, int pageSize)
    {
        return await _dbSet
            .Include(o => o.Items)
            .ThenInclude(i => i.Smartphone)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToPagedListAsync(pageNumber, pageSize);
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(o => o.Items)
            .ThenInclude(i => i.Smartphone)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await _dbSet
            .Include(o => o.Items)
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
}