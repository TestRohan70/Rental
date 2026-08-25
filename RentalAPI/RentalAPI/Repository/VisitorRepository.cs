using Microsoft.EntityFrameworkCore;
using RentalAPI.Constants;
using RentalAPI.DTO;
using RentalAPI.Models;
using RentalAPI.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        var securityRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == AppRoles.Security);
        var securityUser = await _context.SysmUsers.FirstOrDefaultAsync(x =>
            x.Id == dto.SecurityId &&
            x.IsActive &&
            (x.RoleId == (securityRole != null ? securityRole.Id : 4) || x.Role == "Security"));

        if (securityUser is null)
        {
            throw new InvalidOperationException("Only active gate security staff can create visitor requests.");
        }

        var securityUserMapping = await _context.SocietyUserMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == dto.SecurityId && m.IsActive);

        int? securitySocietyId = securityUserMapping?.SocietyId;

        var resident = await FindResidentByUnitAsync(dto.Wing, dto.FlatNo, securitySocietyId);
        if (resident is null)
        {
            throw new InvalidOperationException("No approved Resident found for this wing and flat.");
        }

        var request = new VisitorRequest
        {
            VisitorName = dto.VisitorName.Trim(),
            VisitorPhone = string.IsNullOrWhiteSpace(dto.VisitorPhone) ? null : dto.VisitorPhone.Trim(),
            Purpose = string.IsNullOrWhiteSpace(dto.Purpose) ? null : dto.Purpose.Trim(),
            Wing = dto.Wing.Trim().ToUpperInvariant(),
            FlatNo = dto.FlatNo,
            ResidentId = resident.Id,
            SecurityUserId = securityUser.Id,
            Status = "Pending",
            VisitorPhotoUrl = dto.VisitorPhotoUrl,
            CreatedDate = DateTime.UtcNow
        };

        await _context.VisitorRequests.AddAsync(request);
        await _context.SaveChangesAsync();

        return MapToDto(request, resident, securityUser);
    }

    public async Task<List<VisitorRequestDto>> GetGateRequestsAsync(int securityId)
    {
        var requests = await _context.VisitorRequests
            .AsNoTracking()
            .Include(x => x.Resident)
            .Include(x => x.SecurityUser)
            .Where(x =>
                x.SecurityUserId == securityId &&
                (x.Status == "Pending" || x.Status == "Approved"))
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync();

        return requests.Select(x => MapToDto(x, x.Resident, x.SecurityUser)).ToList();
    }

    public async Task<List<VisitorRequestDto>> GetGateRequestHistoryAsync(int securityId)
    {
        var requests = await _context.VisitorRequests
            .AsNoTracking()
            .Include(x => x.Resident)
            .Include(x => x.SecurityUser)
            .Where(x =>
                x.SecurityUserId == securityId &&
                (x.Status == "Acknowledged" || x.Status == "Rejected"))
            .OrderByDescending(x => x.AcknowledgedDate ?? x.RespondedDate ?? x.CreatedDate)
            .Take(100)
            .ToListAsync();

        return requests.Select(x => MapToDto(x, x.Resident, x.SecurityUser)).ToList();
    }

    public async Task<List<VisitorRequestDto>> GetResidentRequestsAsync(int residentId)
    {
        var resident = await _context.Residents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == residentId);

        if (resident is null)
        {
            return new List<VisitorRequestDto>();
        }

        var requests = await _context.VisitorRequests
            .AsNoTracking()
            .Include(x => x.Resident)
            .Include(x => x.SecurityUser)
            .Where(x => x.ResidentId == residentId)
            .OrderByDescending(x => x.CreatedDate)
            .Take(50)
            .ToListAsync();

        return requests.Select(x => MapToDto(x, x.Resident, x.SecurityUser)).ToList();
    }

    public async Task<VisitorRequestDto?> ApproveAsync(int requestId, int residentId)
    {
        var request = await _context.VisitorRequests
            .Include(x => x.Resident)
            .Include(x => x.SecurityUser)
            .FirstOrDefaultAsync(x => x.Id == requestId && x.ResidentId == residentId);

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

        return MapToDto(request, request.Resident, request.SecurityUser);
    }

    public async Task<VisitorRequestDto?> RejectAsync(int requestId, int residentId)
    {
        var request = await _context.VisitorRequests
            .Include(x => x.Resident)
            .Include(x => x.SecurityUser)
            .FirstOrDefaultAsync(x => x.Id == requestId && x.ResidentId == residentId);

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

        return MapToDto(request, request.Resident, request.SecurityUser);
    }

    public async Task<VisitorRequestDto?> AcknowledgeAsync(int requestId, int securityId)
    {
        var request = await _context.VisitorRequests
            .Include(x => x.Resident)
            .Include(x => x.SecurityUser)
            .FirstOrDefaultAsync(x => x.Id == requestId && x.SecurityUserId == securityId);

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

        return MapToDto(request, request.Resident, request.SecurityUser);
    }

    public async Task<(object? Data, string? ErrorMessage)> LookupResidentAsync(string wing, int flatNo)
    {
        var normalizedWing = wing.Trim().ToUpperInvariant();

        var residentMapping = await _context.ResidentFlatMappings
            .AsNoTracking()
            .Include(m => m.Resident)
            .Include(m => m.SocietyWingFlatConfig)
                .ThenInclude(c => c.Wing)
            .Include(m => m.SocietyWingFlatConfig)
                .ThenInclude(c => c.Flat)
            .Where(m => m.IsActive &&
                        m.SocietyWingFlatConfig.Wing.Name.ToUpper() == normalizedWing &&
                        m.SocietyWingFlatConfig.Flat.Code == flatNo.ToString())
            .FirstOrDefaultAsync();

        if (residentMapping?.Resident != null && residentMapping.Resident.Status == "Approved")
        {
            var res = residentMapping.Resident;
            return (new
            {
                res.Id,
                res.Name,
                Wing = normalizedWing,
                FlatNo = flatNo,
                OwnershipType = residentMapping.OwnershipType
            }, null);
        }

        var directResident = await _context.Residents
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Status == "Approved");

        if (directResident != null)
        {
            return (new
            {
                directResident.Id,
                directResident.Name,
                Wing = normalizedWing,
                FlatNo = flatNo
            }, null);
        }

        return (null, $"No approved resident found for Wing {normalizedWing} and Flat {flatNo}.");
    }

    private async Task<Resident?> FindResidentByUnitAsync(string wing, int flatNo, int? societyId)
    {
        var normalizedWing = wing.Trim().ToUpperInvariant();

        var mapping = await _context.ResidentFlatMappings
            .AsNoTracking()
            .Include(m => m.Resident)
            .Include(m => m.SocietyWingFlatConfig)
                .ThenInclude(c => c.Wing)
            .Include(m => m.SocietyWingFlatConfig)
                .ThenInclude(c => c.Flat)
            .Where(m => m.IsActive &&
                        m.SocietyWingFlatConfig.Wing.Name.ToUpper() == normalizedWing &&
                        m.SocietyWingFlatConfig.Flat.Code == flatNo.ToString() &&
                        m.Resident.Status == "Approved" &&
                        (!societyId.HasValue || m.SocietyWingFlatConfig.SocietyId == societyId.Value))
            .FirstOrDefaultAsync();

        if (mapping?.Resident != null)
        {
            return mapping.Resident;
        }

        return await _context.Residents
            .FirstOrDefaultAsync(r => r.Status == "Approved");
    }

    private static VisitorRequestDto MapToDto(VisitorRequest request, Resident resident, SysmUser securityUser)
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
            ResidentName = resident?.Name ?? "Resident",
            SecurityId = request.SecurityUserId,
            SecurityName = securityUser?.UserName ?? securityUser?.Email ?? "Security Staff",
            Status = request.Status,
            CreatedDate = request.CreatedDate,
            RespondedDate = request.RespondedDate,
            AcknowledgedDate = request.AcknowledgedDate,
            VisitorPhotoUrl = request.VisitorPhotoUrl
        };
    }
}
