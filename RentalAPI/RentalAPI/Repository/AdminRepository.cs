using Microsoft.EntityFrameworkCore;
using RentalAPI.Constants;
using RentalAPI.DTO;
using RentalAPI.Models;
using RentalAPI.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RentalAPI.Repository
{
    public class AdminRepository : IAdminRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminRepository> _logger;

        public AdminRepository(AppDbContext context, ILogger<AdminRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Resident>> GetPendingResidents(int societyId)
        {
            return await _context.Residents
                .Include(r => r.User)
                .Include(r => r.FlatMappings)
                    .ThenInclude(fm => fm.SocietyWingFlatConfig)
                .Where(x => x.Status == "Pending" &&
                            x.FlatMappings.Any(fm => fm.SocietyWingFlatConfig.SocietyId == societyId))
                .ToListAsync();
        }

        public async Task<bool> IsAdmin(int adminId)
        {
            var user = await _context.SysmUsers
                .Include(u => u.RoleNavigation)
                .FirstOrDefaultAsync(x => x.Id == adminId);

            if (user == null || user.RoleNavigation == null)
            {
                return false;
            }

            var code = user.RoleNavigation.Code;
            return code == AppRoles.SuperAdmin || code == AppRoles.SocietyAdmin;
        }

        public async Task<bool> ApproveResident(int residentId)
        {
            var resident = await _context.Residents.FirstOrDefaultAsync(x => x.Id == residentId);
            if (resident == null)
            {
                return false;
            }

            resident.Status = "Approved";
            resident.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectResident(int residentId)
        {
            var resident = await _context.Residents.FirstOrDefaultAsync(x => x.Id == residentId);
            if (resident == null)
            {
                return false;
            }

            resident.Status = "Rejected";
            resident.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SysmUser?> Login(string username, string password)
        {
            var user = await _context.SysmUsers
                .Include(u => u.RoleNavigation)
                .FirstOrDefaultAsync(x => (x.UserName == username || x.Email == username) && x.IsActive);

            if (user == null || string.IsNullOrEmpty(user.Password))
            {
                return null;
            }

            bool valid = false;
            if (user.Password.StartsWith("$2"))
            {
                try
                {
                    valid = BCrypt.Net.BCrypt.Verify(password, user.Password);
                }
                catch { }
            }

            if (!valid && user.Password == password)
            {
                valid = true;
            }

            return valid ? user : null;
        }

        public async Task<SocietyUserMapping> CreateSocietyAdmin(CreateSocietyAdminDto dto)
        {
            var society = await _context.SocietyMasters.FirstOrDefaultAsync(s => s.Id == dto.SocietyId);
            if (society == null)
            {
                throw new KeyNotFoundException("Specified society does not exist.");
            }

            var existingAdminMapping = await _context.SocietyUserMappings
                .Include(m => m.User)
                    .ThenInclude(u => u.RoleNavigation)
                .FirstOrDefaultAsync(m => m.SocietyId == dto.SocietyId &&
                                         m.IsActive &&
                                         m.User != null &&
                                         m.User.IsActive &&
                                         m.User.RoleNavigation != null &&
                                         m.User.RoleNavigation.Code == AppRoles.SocietyAdmin);

            if (existingAdminMapping != null)
            {
                throw new InvalidOperationException("This society already has an assigned Society Admin.");
            }

            var existingUser = await _context.SysmUsers
                .FirstOrDefaultAsync(u => u.Email == dto.Email || u.UserName == dto.UserName);

            if (existingUser != null)
            {
                throw new InvalidOperationException("UserName or Email already exists.");
            }

            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == AppRoles.SocietyAdmin);
            if (adminRole == null)
            {
                throw new InvalidOperationException("SOCIETYADMIN role not found in system.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sysUser = new SysmUser
                {
                    UserName = dto.UserName.Trim(),
                    Email = dto.Email.Trim(),
                    Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    RoleId = adminRole.Id,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                await _context.SysmUsers.AddAsync(sysUser);
                await _context.SaveChangesAsync();

                var mapping = new SocietyUserMapping
                {
                    SocietyId = dto.SocietyId,
                    UserId = sysUser.Id,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                await _context.SocietyUserMappings.AddAsync(mapping);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return mapping;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                if (ex.InnerException?.Message.Contains("SocietyUserMapping", StringComparison.OrdinalIgnoreCase) == true ||
                    ex.InnerException?.Message.Contains("IX_", StringComparison.OrdinalIgnoreCase) == true ||
                    ex.InnerException?.Message.Contains("UX_", StringComparison.OrdinalIgnoreCase) == true ||
                    ex.InnerException?.Message.Contains("UQ_", StringComparison.OrdinalIgnoreCase) == true ||
                    ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true)
                {
                    throw new InvalidOperationException("This society already has an assigned Society Admin.", ex);
                }
                throw;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<SysmUser> RegisterSecurityStaff(int adminUserId, CreateSecurityUserDto dto)
        {
            var adminMapping = await _context.SocietyUserMappings
                .Include(m => m.User)
                    .ThenInclude(u => u.RoleNavigation)
                .FirstOrDefaultAsync(m => m.UserId == adminUserId &&
                                         m.IsActive &&
                                         m.User != null &&
                                         m.User.RoleNavigation != null &&
                                         m.User.RoleNavigation.Code == AppRoles.SocietyAdmin);

            if (adminMapping == null)
            {
                throw new InvalidOperationException("Only an active SocietyAdmin can register gate security staff.");
            }

            var existingUser = await _context.SysmUsers
                .FirstOrDefaultAsync(u => u.Email == dto.Email || (u.UserName != null && u.UserName == dto.Name));

            if (existingUser != null)
            {
                throw new InvalidOperationException("UserName or Email already exists.");
            }

            var securityRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == AppRoles.Security);
            if (securityRole == null)
            {
                throw new InvalidOperationException("SECURITY role not found in system.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var securityUser = new SysmUser
                {
                    UserName = string.IsNullOrWhiteSpace(dto.Name) ? dto.Email.Trim() : dto.Name.Trim(),
                    Email = dto.Email.Trim(),
                    Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    RoleId = securityRole.Id,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                await _context.SysmUsers.AddAsync(securityUser);
                await _context.SaveChangesAsync();

                var mapping = new SocietyUserMapping
                {
                    SocietyId = adminMapping.SocietyId,
                    UserId = securityUser.Id,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                await _context.SocietyUserMappings.AddAsync(mapping);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return securityUser;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<SysmUser>> GetGateSecurityStaff(int adminUserId)
        {
            var adminMapping = await _context.SocietyUserMappings
                .FirstOrDefaultAsync(m => m.UserId == adminUserId && m.IsActive);

            if (adminMapping == null)
            {
                return new List<SysmUser>();
            }

            return await _context.SocietyUserMappings
                .AsNoTracking()
                .Include(m => m.User)
                    .ThenInclude(u => u.RoleNavigation)
                .Where(m => m.SocietyId == adminMapping.SocietyId &&
                            m.IsActive &&
                            m.User != null &&
                            m.User.IsActive &&
                            m.User.RoleNavigation != null &&
                            m.User.RoleNavigation.Code == AppRoles.Security)
                .Select(m => m.User)
                .ToListAsync();
        }
    }
}
