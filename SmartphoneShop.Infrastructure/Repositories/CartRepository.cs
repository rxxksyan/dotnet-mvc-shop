using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Infrastructure.Data;

namespace SmartphoneShop.Infrastructure.Repositories;

public class CartRepository : GenericRepository<Cart>, ICartRepository
{
    public CartRepository(AppDbContext context) : base(context) { }

    public async Task<Cart?> GetByUserIdAsync(string userId)
    {
        return await _dbSet
            .Include(c => c.Items)
            .ThenInclude(i => i.Smartphone)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Cart?> GetBySessionIdAsync(string sessionId)
    {
        return await _dbSet
            .Include(c => c.Items)
            .ThenInclude(i => i.Smartphone)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);
    }

    public async Task AddItemAsync(CartItem item)
    {
        await _context.CartItems.AddAsync(item);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateItemAsync(CartItem item)
    {
        _context.CartItems.Update(item);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(int itemId)
    {
        var item = await _context.CartItems.FindAsync(itemId);
        if (item != null)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ClearAsync(int cartId)
    {
        var items = await _context.CartItems.Where(i => i.CartId == cartId).ToListAsync();
        _context.CartItems.RemoveRange(items);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var cart = await _dbSet.FindAsync(id);
        if (cart != null)
        {
            _dbSet.Remove(cart);
            await _context.SaveChangesAsync();
        }
    }
}
