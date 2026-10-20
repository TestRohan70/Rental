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
        var email = account.Email.Trim();

        var existingUser = await _context.SysmUsers
            .AnyAsync(u => u.Email == email);

        if (existingUser)
            throw new InvalidOperationException("Email already exists.");

        // Fetch Admin role dynamically
        var adminRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Code == "SOCIETYADMIN");

        if (adminRole == null)
            throw new InvalidOperationException("Admin role not found.");

        var user = new SysmUser
        {
            UserName = account.Username.Trim(),
            Email = email,
            Password = account.PasswordHash,
            RoleId = adminRole.Id,
            IsActive = account.IsActive,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = null
        };

        await _context.SysmUsers.AddAsync(user);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task<List<PmAdminAccount>> GetAllAsync()
    {
        return await _context.PmAdminAccounts
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<PmAdminAccount>> GetByIdAsync(int societyId)
    {
        return await _context.PmAdminAccounts.Where(x => x.SocietyID == societyId).ToListAsync();

    }
}