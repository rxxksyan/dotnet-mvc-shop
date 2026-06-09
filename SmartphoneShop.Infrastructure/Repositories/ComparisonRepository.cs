using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Infrastructure.Data;

namespace SmartphoneShop.Infrastructure.Repositories;

public class ComparisonRepository : IComparisonRepository
{
    private readonly AppDbContext _context;

    public ComparisonRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ComparisonList?> GetByUserIdAsync(string userId)
    {
        return await _context.ComparisonLists
            .Include(cl => cl.Items)
            .ThenInclude(ci => ci.Smartphone)
            .FirstOrDefaultAsync(cl => cl.UserId == userId);
    }

    public async Task<ComparisonList?> GetBySessionIdAsync(string sessionId)
    {
        return await _context.ComparisonLists
            .Include(cl => cl.Items)
            .ThenInclude(ci => ci.Smartphone)
            .FirstOrDefaultAsync(cl => cl.SessionId == sessionId);
    }

    public async Task AddAsync(ComparisonList list)
    {
        await _context.ComparisonLists.AddAsync(list);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ComparisonList list)
    {
        _context.ComparisonLists.Update(list);
        await _context.SaveChangesAsync();
    }

    public async Task AddItemAsync(ComparisonItem item)
    {
        await _context.ComparisonItems.AddAsync(item);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(int itemId)
    {
        var item = await _context.ComparisonItems.FindAsync(itemId);
        if (item != null)
        {
            _context.ComparisonItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveItemBySmartphoneIdAsync(int listId, int smartphoneId)
    {
        var item = await _context.ComparisonItems
            .FirstOrDefaultAsync(i => i.ComparisonListId == listId && i.SmartphoneId == smartphoneId);
        if (item != null)
        {
            _context.ComparisonItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ClearAsync(int listId)
    {
        var items = await _context.ComparisonItems.Where(i => i.ComparisonListId == listId).ToListAsync();
        _context.ComparisonItems.RemoveRange(items);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetItemCountAsync(int listId)
    {
        return await _context.ComparisonItems.CountAsync(i => i.ComparisonListId == listId);
    }
}