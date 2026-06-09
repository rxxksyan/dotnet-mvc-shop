using SmartphoneShop.Core.Entities;

namespace SmartphoneShop.Core.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(string userId);
    Task<Cart?> GetBySessionIdAsync(string sessionId);
    Task<Cart?> GetByIdAsync(int id);
    Task AddAsync(Cart cart);
    Task UpdateAsync(Cart cart);
    Task DeleteAsync(int id);
    Task AddItemAsync(CartItem item);
    Task UpdateItemAsync(CartItem item);
    Task RemoveItemAsync(int itemId);
    Task ClearAsync(int cartId);
}