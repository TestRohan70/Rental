using Microsoft.EntityFrameworkCore;
using RentalAPI.Models;
using RentalAPI.Repository.IRepository;

namespace RentalAPI.Repository
{
    public class PmAccountRepository : IPmAccountRepository
    {
        private readonly AppDbContext _context;

        public PmAccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PmAccount>> GetAllAsync()
        {
            return await _context.PmAccounts.AsNoTracking().ToListAsync();
        }

        public async Task<PmAccount?> GetByIdAsync(int id)
        {
            return await _context.PmAccounts.FirstOrDefaultAsync(x => x.ID == id);
        }

        public async Task<PmAccount> CreateAsync(PmAccount account)
        {
            _context.PmAccounts.Add(account);
            await _context.SaveChangesAsync();
            return account;
        }
        public async Task<int> GetMaxIdAsync()
        {
            return await _context.PmAccounts.Select(x => (int?)x.ID).MaxAsync() ?? 0;

        }

        public async Task<PmAccount?> UpdateAsync(int id, PmAccount account)
        {
            var existing = await _context.PmAccounts.FirstOrDefaultAsync(x => x.ID == id);
            if (existing == null)
                return null;

            existing.SocName = account.SocName;
            existing.Code = account.Code;
            existing.Email = account.Email;
            existing.Phone = account.Phone;
            existing.IsActive = account.IsActive;
            existing.SpcDtl = account.SpcDtl;
            existing.ModifiedDate = account.ModifiedDate;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.PmAccounts.FirstOrDefaultAsync(x => x.ID == id);
            if (existing == null)
                return false;

            existing.IsActive = false;
            existing.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
