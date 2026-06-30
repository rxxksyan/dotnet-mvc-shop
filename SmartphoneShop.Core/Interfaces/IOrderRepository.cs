using SmartphoneShop.Core.Entities;
using X.PagedList;

namespace SmartphoneShop.Core.Interfaces;

public interface IOrderRepository
{
    Task<IPagedList<Order>> GetByUserIdPagedAsync(string userId, int pageNumber, int pageSize);
    Task<IEnumerable<Order>> GetByUserIdAsync(string userId);
    Task<Order?> GetByIdAsync(int id);
    Task<IEnumerable<Order>> GetAllAsync();
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task<bool> UserHasPurchasedSmartphoneAsync(string userId, int smartphoneId);
}