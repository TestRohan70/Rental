using RentalAPI.Models;

namespace RentalAPI.Repository.IRepository
{
    public interface IAdminRepository 
    {
        Task<bool> ApproveResident(int residentId);

        Task<bool> RejectResident(int residentId);

        Task<List<Resident>> GetPendingResidents();
        Task<bool> IsAdmin(int adminId);
        Task<SysmUser?> Login(string username, string password);
    }
}
