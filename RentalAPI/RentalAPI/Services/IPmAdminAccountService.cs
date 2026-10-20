using RentalAPI.DTO;
using RentalAPI.Models;

namespace RentalAPI.Services.IServices;

public interface IPmAdminAccountService
{
    Task<PmAdminAccount> CreateAsync(PmAdminAccountCreateDto dto);

    Task<List<PmAdminAccount>> GetAllAsync();
    Task<List<PmAdminAccount>> GetByIdAsync(int societyId);
}