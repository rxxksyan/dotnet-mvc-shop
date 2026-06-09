using SmartphoneShop.Core.Entities;

namespace SmartphoneShop.Core.Interfaces;

public interface IRepairRequestRepository
{
    Task<IEnumerable<RepairRequest>> GetByUserIdAsync(string userId);
    Task<RepairRequest?> GetByIdAsync(int id);
    Task<IEnumerable<RepairRequest>> GetAllAsync();
    Task AddAsync(RepairRequest request);
    Task UpdateAsync(RepairRequest request);
}