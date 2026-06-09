using SmartphoneShop.Core.Entities;
using X.PagedList;

namespace SmartphoneShop.Core.Interfaces;

public interface IReviewRepository
{
    Task<IPagedList<Review>> GetBySmartphoneIdPagedAsync(int smartphoneId, int pageNumber, int pageSize);
    Task<IEnumerable<Review>> GetBySmartphoneIdAsync(int smartphoneId);
    Task<Review?> GetByIdAsync(int id);
    Task AddAsync(Review review);
    Task UpdateAsync(Review review);
    Task DeleteAsync(int id);
    Task<double> GetAverageRatingAsync(int smartphoneId);
    Task<bool> UserHasReviewAsync(string userId, int smartphoneId);
}