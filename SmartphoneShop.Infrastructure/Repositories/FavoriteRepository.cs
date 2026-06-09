using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Infrastructure.Data;

namespace SmartphoneShop.Infrastructure.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly AppDbContext _context;

    public FavoriteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Favorite>> GetByUserIdAsync(string userId)
    {
        return await _context.Favorites
            .Include(f => f.Smartphone)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<Favorite?> GetByUserAndSmartphoneAsync(string userId, int smartphoneId)
    {
        return await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.SmartphoneId == smartphoneId);
    }

    public async Task AddAsync(Favorite favorite)
    {
        await _context.Favorites.AddAsync(favorite);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var favorite = await _context.Favorites.FindAsync(id);
        if (favorite != null)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string userId, int smartphoneId)
    {
        return await _context.Favorites.AnyAsync(f => f.UserId == userId && f.SmartphoneId == smartphoneId);
    }
}