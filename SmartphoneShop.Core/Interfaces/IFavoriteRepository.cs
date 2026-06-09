using SmartphoneShop.Core.Entities;

namespace SmartphoneShop.Core.Interfaces;

public interface IFavoriteRepository
{
    Task<IEnumerable<Favorite>> GetByUserIdAsync(string userId);
    Task<Favorite?> GetByUserAndSmartphoneAsync(string userId, int smartphoneId);
    Task AddAsync(Favorite favorite);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(string userId, int smartphoneId);
}