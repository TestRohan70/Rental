using Microsoft.AspNetCore.Identity;
using RentalAPI.DTO;
using RentalAPI.Models;
using RentalAPI.Repository.IRepository;
using RentalAPI.Services.IServices;

namespace RentalAPI.Services;

public class PmAdminAccountService : IPmAdminAccountService
{
    private readonly IPmAdminAccountRepository _repository;
    private readonly PasswordHasher<PmAdminAccount> _passwordHasher;

    public PmAdminAccountService(
        IPmAdminAccountRepository repository)
    {
        _repository = repository;
        _passwordHasher = new PasswordHasher<PmAdminAccount>();
    }

    public async Task<PmAdminAccount> CreateAsync(PmAdminAccountCreateDto dto)
    {
        var account = new PmAdminAccount
        {
            SocietyID = dto.SocietyID,
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Username = dto.Username,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        account.PasswordHash =
            _passwordHasher.HashPassword(account, dto.Password);

        return await _repository.CreateAsync(account);
    }

    public async Task<List<PmAdminAccount>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<PmAdminAccount?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }
}