using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Infrastructure.Data;
using X.PagedList;

namespace SmartphoneShop.Infrastructure.Repositories;

public class ReviewRepository : GenericRepository<Review>, IReviewRepository
{
    public ReviewRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Review>> GetBySmartphoneIdAsync(int smartphoneId)
    {
        return await _dbSet
            .Include(r => r.User)
            .Where(r => r.SmartphoneId == smartphoneId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IPagedList<Review>> GetBySmartphoneIdPagedAsync(int smartphoneId, int pageNumber, int pageSize)
    {
        return await _dbSet
            .Include(r => r.User)
            .Where(r => r.SmartphoneId == smartphoneId)
            .OrderByDescending(r => r.CreatedAt)
            .ToPagedListAsync(pageNumber, pageSize);
    }

    public async Task<double> GetAverageRatingAsync(int smartphoneId)
    {
        var reviews = await _dbSet.Where(r => r.SmartphoneId == smartphoneId).ToListAsync();
        if (!reviews.Any()) return 0;
        return reviews.Average(r => r.Rating);
    }

    public async Task DeleteAsync(int id)
    {
        var review = await _dbSet.FindAsync(id);
        if (review != null)
        {
            _dbSet.Remove(review);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> UserHasReviewAsync(string userId, int smartphoneId)
    {
        return await _dbSet.AnyAsync(r => r.UserId == userId && r.SmartphoneId == smartphoneId);
    }
}
