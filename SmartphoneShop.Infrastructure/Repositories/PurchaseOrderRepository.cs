using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Infrastructure.Data;
using X.PagedList;

namespace SmartphoneShop.Infrastructure.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly AppDbContext _context;

    public PurchaseOrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
    {
        return await _context.PurchaseOrders.FindAsync(id);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetByUserIdAsync(string userId)
    {
        return await _context.PurchaseOrders
            .Where(po => po.UserId == userId)
            .ToListAsync();
    }

    public async Task<IPagedList<PurchaseOrder>> GetByUserIdPagedAsync(string userId, int pageNumber, int pageSize)
    {
        return await _context.PurchaseOrders
            .Where(po => po.UserId == userId)
            .OrderByDescending(po => po.CreatedAt)
            .ToPagedListAsync(pageNumber, pageSize);
    }

    public async Task AddAsync(PurchaseOrder purchaseOrder)
    {
        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PurchaseOrder purchaseOrder)
    {
        _context.PurchaseOrders.Update(purchaseOrder);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);
        if (purchaseOrder != null)
        {
            _context.PurchaseOrders.Remove(purchaseOrder);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> UserHasPendingOrderAsync(string userId, int smartphoneId)
    {
        return await _context.PurchaseOrders
            .AnyAsync(po => po.UserId == userId
                         && po.SmartphoneId == smartphoneId
                         && (po.Status == "Pending" || po.Status == "Processing"));
    }

    public async Task<bool> UserHasDeliveredOrderAsync(string userId, int smartphoneId)
    {
        return await _context.PurchaseOrders
            .AnyAsync(po => po.UserId == userId
                         && po.SmartphoneId == smartphoneId
                         && po.Status == "Completed");
    }
}