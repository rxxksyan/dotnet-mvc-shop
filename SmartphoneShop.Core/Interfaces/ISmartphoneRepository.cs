using SmartphoneShop.Core.Entities;
using X.PagedList;

namespace SmartphoneShop.Core.Interfaces;

public interface ISmartphoneRepository
{
    Task<IPagedList<Smartphone>> GetPagedAsync(int pageNumber, int pageSize);
    Task<IPagedList<Smartphone>> GetFilteredPagedAsync(string? brand, decimal? minPrice, decimal? maxPrice, int? ram, int? storage, string? sort, string? search, int pageNumber, int pageSize);
    Task<IEnumerable<Smartphone>> GetAllAsync();
    Task<Smartphone?> GetByIdAsync(int id);
    Task<IEnumerable<Smartphone>> GetFeaturedAsync();
    Task<IEnumerable<Smartphone>> SearchAsync(string query);
    Task<IEnumerable<Smartphone>> GetByFilterAsync(string? brand, decimal? minPrice, decimal? maxPrice, int? ram, int? storage);
    Task AddAsync(Smartphone smartphone);
    Task UpdateAsync(Smartphone smartphone);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<IEnumerable<string>> GetDistinctBrandsAsync();
}