using Microsoft.EntityFrameworkCore;
using RentalAPI.Constants;
using RentalAPI.DTO;
using RentalAPI.Models;
using RentalAPI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RentalAPI.Repository
{
    public class ResidentRepository : IResidentRepository
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public ResidentRepository(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<List<Resident>> GetAll()
        {
            return await _context.Residents
                .Include(r => r.User)
                .Include(r => r.FlatMappings)
                    .ThenInclude(fm => fm.SocietyWingFlatConfig)
                        .ThenInclude(cfg => cfg.Society)
                .ToListAsync();
        }

        public async Task<Resident?> GetById(int id)
        {
            return await _context.Residents
                .Include(r => r.User)
                .Include(r => r.FlatMappings)
                    .ThenInclude(fm => fm.SocietyWingFlatConfig)
                        .ThenInclude(cfg => cfg.Society)
                .Include(r => r.FlatMappings)
                    .ThenInclude(fm => fm.SocietyWingFlatConfig)
                        .ThenInclude(cfg => cfg.Wing)
                .Include(r => r.FlatMappings)
                    .ThenInclude(fm => fm.SocietyWingFlatConfig)
                        .ThenInclude(cfg => cfg.Flat)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Resident?> Update(int id, Resident resident)
        {
            var existing = await _context.Residents.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null)
            {
                return null;
            }

            existing.Name = resident.Name;
            existing.Address = resident.Address;
            existing.Parking = resident.Parking;
            existing.NoofParking = resident.NoofParking;
            existing.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> Delete(int id)
        {
            var resident = await _context.Residents
                .Include(r => r.User)
                .Include(r => r.FlatMappings)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (resident == null)
            {
                return false;
            }

            if (resident.FlatMappings.Any())
            {
                _context.ResidentFlatMappings.RemoveRange(resident.FlatMappings);
            }

            _context.Residents.Remove(resident);

            if (resident.User != null)
            {
                _context.SysmUsers.Remove(resident.User);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Resident> Register(CreateResidentDto dto)
        {
            var existingUser = await _context.SysmUsers.FirstOrDefaultAsync(x => x.Email == dto.Email.Trim());
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already exists.");
            }

            var residentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == AppRoles.Resident);
            if (residentRole == null)
            {
                throw new InvalidOperationException("RESIDENT role not found.");
            }

            var sysUser = new SysmUser
            {
                UserName = dto.Email.Trim(),
                Email = dto.Email.Trim(),
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = residentRole.Id,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await _context.SysmUsers.AddAsync(sysUser);
            await _context.SaveChangesAsync();

            var resident = new Resident
            {
                UserId = sysUser.Id,
                Name = dto.Name.Trim(),
                Status = "Pending",
                CreatedDate = DateTime.UtcNow
            };

            await _context.Residents.AddAsync(resident);
            await _context.SaveChangesAsync();

            await _notificationService.CreateResidentRegistrationNotification(resident);
            return resident;
        }

        public async Task<Resident> SelfRegister(SelfRegisterResidentDto dto)
        {
            var rawOwnership = dto.OwnershipType?.Trim() ?? string.Empty;
            string normalizedOwnership;
            if (rawOwnership.Equals("Owner", StringComparison.OrdinalIgnoreCase))
            {
                normalizedOwnership = "Owner";
            }
            else if (rawOwnership.Equals("Tenant", StringComparison.OrdinalIgnoreCase))
            {
                normalizedOwnership = "Tenant";
            }
            else
            {
                throw new ArgumentException("Invalid ownership type. Allowed values: Owner, Tenant.");
            }

            bool isPrimary = normalizedOwnership == "Owner";

            var existingUser = await _context.SysmUsers.FirstOrDefaultAsync(x => x.Email == dto.Email.Trim());
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already exists. Please try with another email.");
            }

            var config = await _context.PmSocietyWingFlatConfigs
                .Include(c => c.Society)
                .Include(c => c.Wing)
                .Include(c => c.Floor)
                .Include(c => c.Flat)
                .FirstOrDefaultAsync(c => c.Id == dto.SocietyWingFlatConfigId && c.IsActive);

            if (config == null || config.Society == null || config.Wing == null || config.Floor == null || config.Flat == null)
            {
                throw new InvalidOperationException("Selected flat configuration is invalid, inactive, or has incomplete relationships.");
            }

            var residentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == AppRoles.Resident);
            if (residentRole == null)
            {
                throw new InvalidOperationException("RESIDENT role not found in system.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (normalizedOwnership == "Owner")
                {
                    var existingOwnerMapping = await _context.ResidentFlatMappings
                        .Include(m => m.Resident)
                        .FirstOrDefaultAsync(m => m.SocietyWingFlatConfigId == config.Id &&
                                                 m.OwnershipType == "Owner" &&
                                                 m.IsActive &&
                                                 m.Resident != null &&
                                                 m.Resident.Status != "Rejected");

                    if (existingOwnerMapping != null)
                    {
                        throw new InvalidOperationException("An active owner is already registered for this flat.");
                    }
                }

                var sysUser = new SysmUser
                {
                    UserName = dto.Email.Trim(),
                    Email = dto.Email.Trim(),
                    Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    RoleId = residentRole.Id,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                await _context.SysmUsers.AddAsync(sysUser);
                await _context.SaveChangesAsync();

                var resident = new Resident
                {
                    UserId = sysUser.Id,
                    Name = dto.Name.Trim(),
                    Status = "Pending",
                    ApprovedBy = null,
                    CreatedDate = DateTime.UtcNow
                };

                await _context.Residents.AddAsync(resident);
                await _context.SaveChangesAsync();

                var mapping = new ResidentFlatMapping
                {
                    ResidentId = resident.Id,
                    SocietyWingFlatConfigId = config.Id,
                    OwnershipType = normalizedOwnership,
                    IsPrimary = isPrimary,
                    StartDate = DateTime.UtcNow,
                    EndDate = null,
                    IsActive = true
                };

                await _context.ResidentFlatMappings.AddAsync(mapping);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                await _notificationService.CreateResidentRegistrationNotification(resident);
                return resident;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Resident> RegisterResidentByAdmin(int adminUserId, CreateResidentUserDto dto)
        {
            var adminMapping = await _context.SocietyUserMappings
                .FirstOrDefaultAsync(m => m.UserId == adminUserId && m.IsActive);

            if (adminMapping == null)
            {
                throw new InvalidOperationException("Only an active SocietyAdmin can register residents.");
            }

            var config = await _context.PmSocietyWingFlatConfigs
                .FirstOrDefaultAsync(c => c.Id == dto.SocietyWingFlatConfigId && c.IsActive);

            if (config == null)
            {
                throw new InvalidOperationException("Selected flat configuration is invalid or inactive.");
            }

            if (config.SocietyId != adminMapping.SocietyId)
            {
                throw new InvalidOperationException("You can only register residents in your assigned society.");
            }

            var existingUser = await _context.SysmUsers.FirstOrDefaultAsync(x => x.Email == dto.Email.Trim());
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already exists.");
            }

            var residentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == AppRoles.Resident);
            if (residentRole == null)
            {
                throw new InvalidOperationException("RESIDENT role not found.");
            }

            var sysUser = new SysmUser
            {
                UserName = dto.Email.Trim(),
                Email = dto.Email.Trim(),
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = residentRole.Id,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await _context.SysmUsers.AddAsync(sysUser);
            await _context.SaveChangesAsync();

            var resident = new Resident
            {
                UserId = sysUser.Id,
                Name = dto.Name.Trim(),
                Address = dto.Address?.Trim(),
                Parking = dto.Parking,
                NoofParking = dto.NoOfParking,
                Status = "Approved",
                ApprovedBy = adminUserId,
                CreatedDate = DateTime.UtcNow
            };

            await _context.Residents.AddAsync(resident);
            await _context.SaveChangesAsync();

            var mapping = new ResidentFlatMapping
            {
                ResidentId = resident.Id,
                SocietyWingFlatConfigId = config.Id,
                OwnershipType = string.IsNullOrWhiteSpace(dto.OwnershipType) ? "Owner" : dto.OwnershipType.Trim(),
                IsPrimary = true,
                StartDate = DateTime.UtcNow,
                IsActive = true
            };

            await _context.ResidentFlatMappings.AddAsync(mapping);
            await _context.SaveChangesAsync();

            return resident;
        }
    }
}