using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Infrastructure.Data;
using X.PagedList;

namespace SmartphoneShop.Infrastructure.Repositories;

public class SmartphoneRepository : GenericRepository<Smartphone>, ISmartphoneRepository
{
    public SmartphoneRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Smartphone>> GetFeaturedAsync()
    {
        return await _dbSet
            .Where(s => s.IsFeatured && s.IsInStock)
            .OrderByDescending(s => s.PopularityScore)
            .Take(8)
            .ToListAsync();
    }

    public async Task<IEnumerable<Smartphone>> SearchAsync(string query)
    {
        var lowerQuery = query.ToLower();
        return await _dbSet
            .Where(s => s.ModelName.ToLower().Contains(lowerQuery) ||
                       s.Brand.ToLower().Contains(lowerQuery) ||
                       (s.Description != null && s.Description.ToLower().Contains(lowerQuery)))
            .OrderByDescending(s => s.PopularityScore)
            .ToListAsync();
    }

    public async Task<IEnumerable<Smartphone>> GetByFilterAsync(string? brand, decimal? minPrice, decimal? maxPrice, int? ram, int? storage)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(brand))
            query = query.Where(s => s.Brand == brand);

        if (minPrice.HasValue)
            query = query.Where(s => s.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(s => s.Price <= maxPrice.Value);

        if (ram.HasValue)
            query = query.Where(s => s.RAM == ram.Value);

        if (storage.HasValue)
            query = query.Where(s => s.Storage == storage.Value);

        return await query
            .Where(s => s.IsInStock)
            .OrderByDescending(s => s.PopularityScore)
            .ToListAsync();
    }

    public async Task<IPagedList<Smartphone>> GetPagedAsync(int pageNumber, int pageSize)
    {
        return await _dbSet
            .OrderByDescending(s => s.CreatedAt)
            .ToPagedListAsync(pageNumber, pageSize);
    }

    public async Task<IPagedList<Smartphone>> GetFilteredPagedAsync(string? brand, decimal? minPrice, decimal? maxPrice, int? ram, int? storage, string? sort, string? search, int pageNumber, int pageSize)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(brand))
            query = query.Where(s => s.Brand == brand);

        if (minPrice.HasValue)
            query = query.Where(s => s.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(s => s.Price <= maxPrice.Value);

        if (ram.HasValue)
            query = query.Where(s => s.RAM == ram.Value);

        if (storage.HasValue)
            query = query.Where(s => s.Storage == storage.Value);

        if (!string.IsNullOrEmpty(search))
        {
            var lowerSearch = search.ToLower();
            query = query.Where(s => s.ModelName.ToLower().Contains(lowerSearch) ||
                                     s.Brand.ToLower().Contains(lowerSearch) ||
                                     (s.Description != null && s.Description.ToLower().Contains(lowerSearch)));
        }

        query = sort switch
        {
            "price_asc" => query.OrderBy(s => s.Price),
            "price_desc" => query.OrderByDescending(s => s.Price),
            "name_asc" => query.OrderBy(s => s.ModelName),
            "name_desc" => query.OrderByDescending(s => s.ModelName),
            _ => query.OrderByDescending(s => s.CreatedAt)
        };

        return await query
            .Where(s => s.IsInStock)
            .ToPagedListAsync(pageNumber, pageSize);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _dbSet.AnyAsync(s => s.Id == id);
    }

    public async Task DeleteAsync(int id)
    {
        var smartphone = await _dbSet.FindAsync(id);
        if (smartphone != null)
        {
            _dbSet.Remove(smartphone);
            await _context.SaveChangesAsync();
        }
    }
}
