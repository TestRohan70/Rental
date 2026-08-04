using RentalAPI.DTO;
using RentalAPI.Models;

namespace RentalAPI.Repository.IRepository;

public interface ISocietyAlertRepository
{
    Task<SocietyAlert> CreateAsync(int createdBySecurityId, CreateSocietyAlertDto dto);

    Task<List<SocietyAlert>> GetAllAsync();

    Task<List<SocietyAlert>> GetBySecurityIdAsync(int securityId);
}
