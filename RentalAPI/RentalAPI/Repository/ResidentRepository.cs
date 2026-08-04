using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalAPI.DTO;
using RentalAPI.Models;
using RentalAPI.Services;


namespace RentalAPI.Repository
{
    public class ResidentRepository : IResidentRepository
    {
        private static readonly string[] AllowedRoles = ["Security", "Tenant", "Owner"];

        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public ResidentRepository(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<List<Resident>> GetAll()
        {
            return await _context.Residents.ToListAsync();

        }

        public async Task<Resident?> GetById(int id)
        {
            return await _context.Residents.FirstOrDefaultAsync(x => x.Id == id);
        }
        public async Task<Resident?> Update(int id, Resident resident)
        {
            var existing = await _context.Residents.FirstOrDefaultAsync(x => x.Id == id);

            if (existing == null)
                return null;

            existing.Name = resident.Name;
            existing.Email = resident.Email;
            existing.Wing = resident.Wing;
            existing.FlatNo = resident.FlatNo;
            existing.Address = resident.Address;
            existing.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return existing;
        }

        public async Task<bool> Delete(int id)
        {
            var resident = await _context.Residents.FirstOrDefaultAsync(x => x.Id == id);

            if (resident == null)
                return false;

            _context.Residents.Remove(resident);
            await _context.SaveChangesAsync();
            return true;
        }

        
        public async Task<Resident> Register(CreateResidentDto dto)
        {
            var existingResident = await _context.Residents.FirstOrDefaultAsync(x => x.Email == dto.Email);
            if (existingResident != null)
            {
                throw new InvalidOperationException("Email already exists.");
            }

            var role = NormalizeRole(dto.Role);
            if (role is null)
            {
                throw new InvalidOperationException("Please select a valid role.");
            }

            var isSecurity = role == "Security";

            if (!isSecurity)
            {
                if (string.IsNullOrWhiteSpace(dto.Wing))
                {
                    throw new InvalidOperationException("Please select your wing.");
                }

                if (dto.FlatNo < 1)
                {
                    throw new InvalidOperationException("Enter a valid flat number.");
                }
            }

            var resident = new Resident
            {
                Name = dto.Name,
                Email = dto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Wing = isSecurity ? "—" : dto.Wing.Trim(),
                FlatNo = isSecurity ? 0 : dto.FlatNo,
                Role = role,
                Status = "Pending",
                CreatedDate = DateTime.UtcNow
            };

            await _context.Residents.AddAsync(resident);
            await _context.SaveChangesAsync();
            await _notificationService.CreateResidentRegistrationNotification( resident);
            return resident;
        }

        public async Task<Resident> RegisterSecurityByAdmin(int adminId, RegisterSecurityStaffDto dto)
        {
            var admin = await _context.SysmUsers.FirstOrDefaultAsync(x =>
                x.Id == adminId &&
                x.Role == "Admin");

            if (admin is null)
            {
                throw new InvalidOperationException("Only administrators can register gate security.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new InvalidOperationException("Enter the security staff name.");
            }

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new InvalidOperationException("Enter a valid email address.");
            }

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            {
                throw new InvalidOperationException("Password must be at least 8 characters.");
            }

            var existingResident = await _context.Residents.FirstOrDefaultAsync(x => x.Email == dto.Email.Trim());
            if (existingResident != null)
            {
                throw new InvalidOperationException("Email already exists.");
            }

            var resident = new Resident
            {
                Name = dto.Name.Trim(),
                Email = dto.Email.Trim(),
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Wing = "—",
                FlatNo = 0,
                Role = "Security",
                Status = "Approved",
                ApprovedBy = adminId,
                CreatedDate = DateTime.UtcNow
            };

            await _context.Residents.AddAsync(resident);
            await _context.SaveChangesAsync();
            return resident;
        }

        public async Task<List<Resident>> GetGateSecurityStaff()
        {
            return await _context.Residents
                .Where(x => x.Role == "Security")
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }


        public async Task<Resident?> Login(string userName, string password)
        {
            var login = userName.Trim();

            var resident = await _context.Residents
                .FirstOrDefaultAsync(x =>
                    (x.Name == login || x.Email == login) &&
                    x.Status == "Approved");

            if (resident == null)
                return null;

            // Password verify karo
            bool isValid = BCrypt.Net.BCrypt.Verify(
                password,
                resident.Password);

            return isValid ? resident : null;
        }

        private static string? NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return null;
            }

            return AllowedRoles.FirstOrDefault(
                allowed => allowed.Equals(role.Trim(), StringComparison.OrdinalIgnoreCase));
        }

    }
}