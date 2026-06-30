using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Infrastructure.Data;
using X.PagedList;

namespace SmartphoneShop.Infrastructure.Repositories;

public class SparePartRepository : GenericRepository<SparePart>, ISparePartRepository
{
    public SparePartRepository(AppDbContext context) : base(context) { }

    public async Task<IPagedList<SparePart>> GetPagedAsync(int pageNumber, int pageSize)
    {
        return await _dbSet
            .OrderByDescending(s => s.CreatedAt)
            .ToPagedListAsync(pageNumber, pageSize);
    }

    public async Task<IPagedList<SparePart>> SearchAsync(string? search, int pageNumber, int pageSize)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(lowerSearch));
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .ToPagedListAsync(pageNumber, pageSize);
    }

    public async Task DeleteAsync(int id)
    {
        var sparePart = await _dbSet.FindAsync(id);
        if (sparePart != null)
        {
            _dbSet.Remove(sparePart);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetCountAsync()
    {
        return await _dbSet.CountAsync();
    }
}
