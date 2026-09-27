using RentalAPI.Models;

namespace RentalAPI.Services
{
    public interface IPmAccountService
    {
        Task<List<PmAccount>> GetAllAsync();

        Task<PmAccount?> GetByIdAsync(int id);

        Task<PmAccount> CreateAsync(PmAccount account);

        Task<PmAccount?> UpdateAsync(int id, PmAccount account);

        Task<bool> DeleteAsync(int id);
    }
}
