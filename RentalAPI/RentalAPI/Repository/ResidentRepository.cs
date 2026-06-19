using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalAPI.DTO;
using RentalAPI.Models;
using RentalAPI.Services;


namespace RentalAPI.Repository
{
    public class ResidentRepository : IResidentRepository
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public ResidentRepository(AppDbContext context, INotificationService _notificationService)
        {
            _context = context;
            _notificationService = _notificationService;
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

            var resident = new Resident
            {
                Name = dto.Name,
                Email = dto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Wing = dto.Wing,
                FlatNo = dto.FlatNo,
                Status = "Pending",
                CreatedDate = DateTime.UtcNow
            };

            await _context.Residents.AddAsync(resident);
            await _context.SaveChangesAsync();
            await _notificationService.CreateResidentRegistrationNotification( resident);
               


            return resident;
        }

    }
}