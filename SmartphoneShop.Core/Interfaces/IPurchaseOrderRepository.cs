using SmartphoneShop.Core.Entities;
using X.PagedList;

namespace SmartphoneShop.Core.Interfaces;

public interface IPurchaseOrderRepository
{
    Task<IPagedList<PurchaseOrder>> GetByUserIdPagedAsync(string userId, int pageNumber, int pageSize);
    Task<PurchaseOrder?> GetByIdAsync(int id);
    Task<IEnumerable<PurchaseOrder>> GetByUserIdAsync(string userId);
    Task AddAsync(PurchaseOrder purchaseOrder);
    Task UpdateAsync(PurchaseOrder purchaseOrder);
    Task DeleteAsync(int id);
    Task<bool> UserHasPendingOrderAsync(string userId, int smartphoneId);
}