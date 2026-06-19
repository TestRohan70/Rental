using Microsoft.EntityFrameworkCore;

using RentalAPI.Models;
using RentalAPI.Repository.IRepository;

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

        public async Task<List<Resident>> GetPendingResidents()
        {
            return await _context.Residents.Where(x => x.Status == "Pending").ToListAsync();
        }



        public async Task<bool> ApproveResident(int residentId)
        {
            var resident = await _context.Residents.FirstOrDefaultAsync(x => x.Id == residentId);
            if (resident == null)
            {
                return false;
            }

            resident.Status = "Approved";

            resident.UpdatedDate =
                DateTime.UtcNow;

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

    }
}
