using Microsoft.EntityFrameworkCore;
using RentalAPI.DTO;
using RentalAPI.Models;
using RentalAPI.Repository.IRepository;

namespace RentalAPI.Repository;

public class SocietyAlertRepository : ISocietyAlertRepository
{
    private static readonly string[] AllowedAlertTypes = ["Emergency", "General", "Maintenance"];

    private readonly AppDbContext _context;

    public SocietyAlertRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SocietyAlert> CreateAsync(int createdBySecurityId, CreateSocietyAlertDto dto)
    {
        var security = await _context.Residents.FirstOrDefaultAsync(x =>
            x.Id == createdBySecurityId &&
            x.Status == "Approved" &&
            x.Role == "Security");

        if (security is null)
        {
            throw new InvalidOperationException("Only approved secretary or security staff can generate alerts.");
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new InvalidOperationException("Alert title is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Message))
        {
            throw new InvalidOperationException("Alert message is required.");
        }

        var alertType = NormalizeAlertType(dto.AlertType);

        var alert = new SocietyAlert
        {
            Title = dto.Title.Trim(),
            Message = dto.Message.Trim(),
            AlertType = alertType,
            CreatedBySecurityId = createdBySecurityId,
            CreatedDate = DateTime.UtcNow
        };

        await _context.SocietyAlerts.AddAsync(alert);
        await _context.SaveChangesAsync();
        return alert;
    }

    public async Task<List<SocietyAlert>> GetAllAsync()
    {
        return await _context.SocietyAlerts
            .Include(x => x.CreatedBySecurity)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync();
    }

    public async Task<List<SocietyAlert>> GetBySecurityIdAsync(int securityId)
    {
        var security = await _context.Residents.FirstOrDefaultAsync(x =>
            x.Id == securityId &&
            x.Status == "Approved" &&
            x.Role == "Security");

        if (security is null)
        {
            throw new InvalidOperationException("Only approved secretary or security staff can view alerts.");
        }

        return await GetAllAsync();
    }

    private static string NormalizeAlertType(string? alertType)
    {
        if (string.IsNullOrWhiteSpace(alertType))
        {
            return "General";
        }

        return AllowedAlertTypes.FirstOrDefault(
            allowed => allowed.Equals(alertType.Trim(), StringComparison.OrdinalIgnoreCase)) ?? "General";
    }
}
