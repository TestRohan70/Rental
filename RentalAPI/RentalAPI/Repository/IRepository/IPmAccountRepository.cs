using RentalAPI.Models;

namespace RentalAPI.Repository.IRepository
{
    public interface IPmAccountRepository
    {
        Task<List<PmAccount>> GetAllAsync();

        Task<PmAccount?> GetByIdAsync(int id);

        Task<PmAccount> CreateAsync(PmAccount account);

        Task<PmAccount?> UpdateAsync(int id, PmAccount account);
        Task<int> GetMaxIdAsync();
        Task<bool> DeleteAsync(int id);
    }
}
