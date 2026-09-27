using RentalAPI.Models;

namespace RentalAPI.Repository.IRepository
{
    public interface IPmAdminAccountRepository
    {
        Task<PmAdminAccount> CreateAsync(PmAdminAccount account);

        Task<List<PmAdminAccount>> GetAllAsync();

        Task<PmAdminAccount?> GetByIdAsync(int id);
    }
}
