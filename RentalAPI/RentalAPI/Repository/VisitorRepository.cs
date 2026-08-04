using Microsoft.EntityFrameworkCore;
using RentalAPI.DTO;
using RentalAPI.Models;
using RentalAPI.Repository.IRepository;

namespace RentalAPI.Repository;

public class VisitorRepository : IVisitorRepository
{
    private readonly AppDbContext _context;

    public VisitorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<VisitorRequestDto> CreateAsync(CreateVisitorRequestDto dto)
    {
        var security = await _context.Residents.FirstOrDefaultAsync(x =>
            x.Id == dto.SecurityId &&
            x.Status == "Approved" &&
            x.Role == "Security");

        if (security is null)
        {
            throw new InvalidOperationException("Only approved security staff can create visitor requests.");
        }

        var resident = await FindResidentByUnitAsync(dto.Wing, dto.FlatNo);
        if (resident is null)
        {
            throw new InvalidOperationException("No approved Tenant or Owner found for this wing and flat.");
        }

        var request = new VisitorRequest
        {
            VisitorName = dto.VisitorName.Trim(),
            VisitorPhone = string.IsNullOrWhiteSpace(dto.VisitorPhone) ? null : dto.VisitorPhone.Trim(),
            Purpose = string.IsNullOrWhiteSpace(dto.Purpose) ? null : dto.Purpose.Trim(),
            Wing = dto.Wing.Trim().ToUpperInvariant(),
            FlatNo = dto.FlatNo,
            ResidentId = resident.Id,
            SecurityId = security.Id,
            Status = "Pending",
            VisitorPhotoUrl = dto.VisitorPhotoUrl,
            CreatedDate = DateTime.UtcNow
        };

        await _context.VisitorRequests.AddAsync(request);
        await _context.SaveChangesAsync();

        return MapToDto(request, resident, security);
    }

    public async Task<List<VisitorRequestDto>> GetGateRequestsAsync(int securityId)
    {
        var requests = await _context.VisitorRequests
            .AsNoTracking()
            .Include(x => x.Resident)
            .Include(x => x.Security)
            .Where(x =>
                x.SecurityId == securityId &&
                (x.Status == "Pending" || x.Status == "Approved"))
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync();

        return requests.Select(x => MapToDto(x, x.Resident, x.Security)).ToList();
    }

    public async Task<List<VisitorRequestDto>> GetGateRequestHistoryAsync(int securityId)
    {
        var requests = await _context.VisitorRequests
            .AsNoTracking()
            .Include(x => x.Resident)
            .Include(x => x.Security)
            .Where(x =>
                x.SecurityId == securityId &&
                (x.Status == "Acknowledged" || x.Status == "Rejected"))
            .OrderByDescending(x => x.AcknowledgedDate ?? x.RespondedDate ?? x.CreatedDate)
            .Take(100)
            .ToListAsync();

        return requests.Select(x => MapToDto(x, x.Resident, x.Security)).ToList();
    }

    public async Task<List<VisitorRequestDto>> GetResidentRequestsAsync(int residentId)
    {
        var resident = await _context.Residents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == residentId);

        if (resident is null || !CanManageVisitorRequests(resident))
        {
            return new List<VisitorRequestDto>();
        }

        var normalizedWing = resident.Wing.Trim().ToUpperInvariant();

        var requests = await _context.VisitorRequests
            .AsNoTracking()
            .Include(x => x.Resident)
            .Include(x => x.Security)
            .Where(x =>
                x.ResidentId == residentId ||
                (x.FlatNo == resident.FlatNo && x.Wing.ToUpper() == normalizedWing))
            .OrderByDescending(x => x.CreatedDate)
            .Take(50)
            .ToListAsync();

        return requests.Select(x => MapToDto(x, x.Resident, x.Security)).ToList();
    }

    public async Task<VisitorRequestDto?> ApproveAsync(int requestId, int residentId)
    {
        var request = await GetTrackedRequestAsync(requestId, residentId);
        if (request is null)
        {
            return null;
        }

        if (request.Status != "Pending")
        {
            throw new InvalidOperationException("Only pending requests can be approved.");
        }

        request.Status = "Approved";
        request.RespondedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(request, request.Resident, request.Security);
    }

    public async Task<VisitorRequestDto?> RejectAsync(int requestId, int residentId)
    {
        var request = await GetTrackedRequestAsync(requestId, residentId);
        if (request is null)
        {
            return null;
        }

        if (request.Status != "Pending")
        {
            throw new InvalidOperationException("Only pending requests can be rejected.");
        }

        request.Status = "Rejected";
        request.RespondedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(request, request.Resident, request.Security);
    }

    public async Task<VisitorRequestDto?> AcknowledgeAsync(int requestId, int securityId)
    {
        var request = await _context.VisitorRequests
            .Include(x => x.Resident)
            .Include(x => x.Security)
            .FirstOrDefaultAsync(x => x.Id == requestId && x.SecurityId == securityId);

        if (request is null)
        {
            return null;
        }

        if (request.Status != "Approved")
        {
            throw new InvalidOperationException("Only approved requests can be acknowledged.");
        }

        request.Status = "Acknowledged";
        request.AcknowledgedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(request, request.Resident, request.Security);
    }

    public async Task<(object? Data, string? ErrorMessage)> LookupResidentAsync(string wing, int flatNo)
    {
        var normalizedWing = wing.Trim().ToUpperInvariant();

        var unitResidents = await _context.Residents
            .AsNoTracking()
            .Where(x => x.FlatNo == flatNo && x.Wing != null && x.Wing.ToUpper() == normalizedWing)
            .ToListAsync();

        if (unitResidents.Count == 0)
        {
            return (null, $"No resident registered for Wing {normalizedWing} and Flat {flatNo}.");
        }

        var match = unitResidents.FirstOrDefault(IsApprovedTenantOrOwner);
        if (match is not null)
        {
            return (new
            {
                match.Id,
                match.Name,
                match.Wing,
                match.FlatNo,
                match.Role
            }, null);
        }

        var pending = unitResidents.FirstOrDefault(x =>
            string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase));
        if (pending is not null)
        {
            return (null, $"{pending.Name} is registered but waiting for admin approval.");
        }

        var rejected = unitResidents.FirstOrDefault(x =>
            string.Equals(x.Status, "Rejected", StringComparison.OrdinalIgnoreCase));
        if (rejected is not null)
        {
            return (null, $"{rejected.Name}'s registration was rejected by admin.");
        }

        var resident = unitResidents[0];
        var roleLabel = string.IsNullOrWhiteSpace(resident.Role) ? "missing role" : resident.Role;
        var statusLabel = string.IsNullOrWhiteSpace(resident.Status) ? "unknown" : resident.Status;

        return (null, $"Resident found ({resident.Name}) but must be an approved Tenant or Owner (current role: {roleLabel}, status: {statusLabel}).");
    }

    private async Task<VisitorRequest?> GetTrackedRequestAsync(int requestId, int residentId)
    {
        var resident = await _context.Residents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == residentId);

        if (resident is null || !CanManageVisitorRequests(resident))
        {
            return null;
        }

        var normalizedWing = resident.Wing.Trim().ToUpperInvariant();

        return await _context.VisitorRequests
            .Include(x => x.Resident)
            .Include(x => x.Security)
            .FirstOrDefaultAsync(x =>
                x.Id == requestId &&
                (x.ResidentId == residentId ||
                 (x.FlatNo == resident.FlatNo && x.Wing.ToUpper() == normalizedWing)));
    }

    private async Task<Resident?> FindResidentByUnitAsync(string wing, int flatNo)
    {
        var normalizedWing = wing.Trim().ToUpperInvariant();

        var unitResidents = await _context.Residents
            .Where(x => x.FlatNo == flatNo && x.Wing != null && x.Wing.ToUpper() == normalizedWing)
            .ToListAsync();

        return unitResidents.FirstOrDefault(IsApprovedTenantOrOwner);
    }

    private static bool IsApprovedTenantOrOwner(Resident resident) =>
        string.Equals(resident.Status, "Approved", StringComparison.OrdinalIgnoreCase) &&
        (string.Equals(resident.Role, "Tenant", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(resident.Role, "Owner", StringComparison.OrdinalIgnoreCase));

    private static bool CanManageVisitorRequests(Resident resident)
    {
        if (resident.FlatNo < 1)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(resident.Wing) || resident.Wing is "—" or "-")
        {
            return false;
        }

        if (!string.Equals(resident.Status, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !string.Equals(resident.Role, "Security", StringComparison.OrdinalIgnoreCase);
    }

    private static VisitorRequestDto MapToDto(VisitorRequest request, Resident resident, Resident security)
    {
        return new VisitorRequestDto
        {
            Id = request.Id,
            VisitorName = request.VisitorName,
            VisitorPhone = request.VisitorPhone,
            Purpose = request.Purpose,
            Wing = request.Wing,
            FlatNo = request.FlatNo,
            ResidentId = request.ResidentId,
            ResidentName = resident.Name,
            SecurityId = request.SecurityId,
            SecurityName = security.Name,
            Status = request.Status,
            CreatedDate = request.CreatedDate,
            RespondedDate = request.RespondedDate,
            AcknowledgedDate = request.AcknowledgedDate,
            VisitorPhotoUrl = request.VisitorPhotoUrl
        };
    }
}
