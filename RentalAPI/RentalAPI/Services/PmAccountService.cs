using Microsoft.EntityFrameworkCore;
using RentalAPI.Models;
using RentalAPI.Repository.IRepository;

namespace RentalAPI.Services
{
    public class PmAccountService : IPmAccountService
    {
        private readonly IPmAccountRepository _repository;

        public PmAccountService(IPmAccountRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<PmAccount>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<PmAccount?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<PmAccount> CreateAsync(PmAccount account)
        {
            account.CreatedDate = DateTime.UtcNow;
            account.IsActive = true;

            var maxId = await _repository.GetMaxIdAsync();

            // Generate Code using generated ID
            account.ID = maxId + 1;
            account.Code = $"PMSOC{account.ID}";

            return await _repository.CreateAsync(account);

        }

        

        public async Task<PmAccount?> UpdateAsync(
            int id,
            PmAccount account)
        {
            account.ModifiedDate = DateTime.UtcNow;

            return await _repository.UpdateAsync(id, account);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
