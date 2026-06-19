using RentalAPI.DTO;
using RentalAPI.Models;


namespace RentalAPI.Repository
{
    public interface IResidentRepository
    {
        Task<List<Resident>> GetAll();

        Task<Resident?> GetById(int id);

        //Task<Resident> Add(CreateResidentDto dto);
        Task<Resident?> Update(int id, Resident resident);
        Task<Resident> Register(CreateResidentDto dto);


        Task<bool> Delete(int id);
    }
}