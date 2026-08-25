using RentalAPI.DTO;
using RentalAPI.Models;
using RentalAPI.Repository.IRepository;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RentalAPI.Repository;

public class SocietyAlertRepository : ISocietyAlertRepository
{
    public Task<SocietyAlert> CreateAsync(int createdBySecurityId, CreateSocietyAlertDto dto)
    {
        throw new NotSupportedException("SocietyAlert functionality is not present in the current database schema.");
    }

    public Task<List<SocietyAlert>> GetAllAsync()
    {
        return Task.FromResult(new List<SocietyAlert>());
    }

    public Task<List<SocietyAlert>> GetBySecurityIdAsync(int securityId)
    {
        return Task.FromResult(new List<SocietyAlert>());
    }
}
