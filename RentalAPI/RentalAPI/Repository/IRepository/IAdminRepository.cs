using RentalAPI.DTO;
using RentalAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RentalAPI.Repository.IRepository
{
    public interface IAdminRepository
    {
        Task<bool> ApproveResident(int residentId);

        Task<bool> RejectResident(int residentId);

        Task<List<Resident>> GetPendingResidents();

        Task<bool> IsAdmin(int adminId);

        Task<SysmUser?> Login(string username, string password);

        Task<SocietyUserMapping> CreateSocietyAdmin(CreateSocietyAdminDto dto);

        Task<SysmUser> RegisterSecurityStaff(int adminUserId, CreateSecurityUserDto dto);

        Task<List<SysmUser>> GetGateSecurityStaff(int adminUserId);
    }
}
