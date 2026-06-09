using SmartphoneShop.Core.Entities;

namespace SmartphoneShop.Core.Interfaces;

public interface IComparisonRepository
{
    Task<ComparisonList?> GetByUserIdAsync(string userId);
    Task<ComparisonList?> GetBySessionIdAsync(string sessionId);
    Task AddAsync(ComparisonList list);
    Task UpdateAsync(ComparisonList list);
    Task AddItemAsync(ComparisonItem item);
    Task RemoveItemAsync(int itemId);
    Task RemoveItemBySmartphoneIdAsync(int listId, int smartphoneId);
    Task ClearAsync(int listId);
    Task<int> GetItemCountAsync(int listId);
}