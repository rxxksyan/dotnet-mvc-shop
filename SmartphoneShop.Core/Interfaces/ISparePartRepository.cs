using SmartphoneShop.Core.Entities;
using X.PagedList;

namespace SmartphoneShop.Core.Interfaces;

public interface ISparePartRepository
{
    Task<IPagedList<SparePart>> GetPagedAsync(int pageNumber, int pageSize);
    Task<IPagedList<SparePart>> SearchAsync(string? search, int pageNumber, int pageSize);
    Task<SparePart?> GetByIdAsync(int id);
    Task AddAsync(SparePart sparePart);
    Task UpdateAsync(SparePart sparePart);
    Task DeleteAsync(int id);
    Task<int> GetCountAsync();
}
