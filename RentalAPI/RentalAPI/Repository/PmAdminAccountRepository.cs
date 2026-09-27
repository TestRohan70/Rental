using Microsoft.EntityFrameworkCore;
using RentalAPI.Models;
using RentalAPI.Repository.IRepository;

namespace RentalAPI.Repository;

public class PmAdminAccountRepository : IPmAdminAccountRepository
{
    private readonly AppDbContext _context;

    public PmAdminAccountRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PmAdminAccount> CreateAsync(PmAdminAccount account)
    {
        _context.PmAdminAccounts.Add(account);

        await _context.SaveChangesAsync();

        return account;
    }

    public async Task<List<PmAdminAccount>> GetAllAsync()
    {
        return await _context.PmAdminAccounts
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<PmAdminAccount?> GetByIdAsync(int id)
    {
        return await _context.PmAdminAccounts
            .FirstOrDefaultAsync(x => x.ID == id);
    }
}