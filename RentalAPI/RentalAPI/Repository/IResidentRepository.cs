using RentalAPI.DTO;
using RentalAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RentalAPI.Repository
{
    public interface IResidentRepository
    {
        Task<List<Resident>> GetAll();

        Task<Resident?> GetById(int id);

        Task<Resident?> Update(int id, Resident resident);

        Task<Resident> Register(CreateResidentDto dto);

        Task<Resident> RegisterResidentByAdmin(int adminUserId, CreateResidentUserDto dto);

        Task<Resident> SelfRegister(SelfRegisterResidentDto dto);

        Task<bool> Delete(int id);
    }
}