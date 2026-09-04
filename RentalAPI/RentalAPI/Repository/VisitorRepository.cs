using Microsoft.EntityFrameworkCore;
using RentalAPI.Constants;
using RentalAPI.DTO;
using RentalAPI.Models;
using RentalAPI.Repository.IRepository;
using RentalAPI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace RentalAPI.Repository;

public class VisitorRepository : IVisitorRepository
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;

    public VisitorRepository(AppDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<PlannedVisitorResponseDto> CreatePlannedAsync(int currentUserId, CreatePlannedVisitorRequestDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (string.IsNullOrWhiteSpace(dto.VisitorName))
        {
            throw new InvalidOperationException("Visitor name is required.");
        }

        var resident = await GetResidentBySysmUserIdAsync(currentUserId);
        if (resident == null)
        {
            throw new InvalidOperationException("Authenticated user is not registered as a resident.");
        }

        var activeMappings = await _context.ResidentFlatMappings
            .AsNoTracking()
            .Include(m => m.SocietyWingFlatConfig)
                .ThenInclude(c => c.Wing)
            .Include(m => m.SocietyWingFlatConfig)
                .ThenInclude(c => c.Flat)
            .Where(m => m.ResidentId == resident.Id && m.IsActive)
            .ToListAsync();

        if (activeMappings.Count == 0)
        {
            throw new InvalidOperationException("No active flat is mapped to the authenticated resident.");
        }

        ResidentFlatMapping? resolvedMapping = null;
        var primaryMappings = activeMappings.Where(m => m.IsPrimary).ToList();

        if (primaryMappings.Count == 1)
        {
            resolvedMapping = primaryMappings[0];
        }
        else if (primaryMappings.Count > 1)
        {
            throw new InvalidOperationException("Multiple primary flats found for the authenticated resident. Please contact administration.");
        }
        else
        {
            if (activeMappings.Count == 1)
            {
                resolvedMapping = activeMappings[0];
            }
            else
            {
                throw new InvalidOperationException("Multiple active flats found for the authenticated resident. Please configure a primary flat.");
            }
        }

        if (resolvedMapping == null || resolvedMapping.SocietyWingFlatConfig == null)
        {
            throw new InvalidOperationException("Unable to resolve active flat configuration for the authenticated resident.");
        }

        var approvedStatus = await GetStatusByCodeAsync(VisitorStatusConstants.Approved);
        var societyId = resolvedMapping.SocietyWingFlatConfig.SocietyId;

        // Generate 6-digit OTP
        var plainOtp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var otpHash = BCrypt.Net.BCrypt.HashPassword(plainOtp);
        var otpExpiresAt = DateTime.UtcNow.AddHours(24);

        var wingName = resolvedMapping.SocietyWingFlatConfig.Wing?.Name ?? string.Empty;
        var flatCode = resolvedMapping.SocietyWingFlatConfig.Flat?.Code ?? string.Empty;
        int.TryParse(flatCode, out int flatNo);

        var request = new VisitorRequest
        {
            VisitorName = dto.VisitorName.Trim(),
            VisitorPhone = string.IsNullOrWhiteSpace(dto.VisitorPhone) ? null : dto.VisitorPhone.Trim(),
            Purpose = string.IsNullOrWhiteSpace(dto.Purpose) ? null : dto.Purpose.Trim(),
            Wing = wingName,
            FlatNo = flatNo,
            ResidentId = resident.Id,
            SecurityUserId = null,
            SocietyId = societyId,
            SocietyWingFlatConfigId = resolvedMapping.SocietyWingFlatConfigId,
            VisitType = "PLANNED",
            ExpectedArrivalDateTime = dto.ExpectedArrivalDateTime ?? DateTime.UtcNow,
            OTPHash = otpHash,
            OTPExpiresAt = otpExpiresAt,
            OTPVerifiedAt = null,
            StatusId = approvedStatus.Id,
            CreatedDate = DateTime.UtcNow
        };

        await _context.VisitorRequests.AddAsync(request);
        await _context.SaveChangesAsync();

        await _notificationService.CreatePlannedVisitorCreatedNotification(request);

        return new PlannedVisitorResponseDto
        {
            Message = "Planned visitor pass created successfully",
            VisitorRequestId = request.Id,
            Otp = plainOtp,
            OtpExpiresAt = otpExpiresAt
        };
    }

    public async Task<VisitorRequestResponseDto> CreateUnplannedAsync(int currentUserId, CreateUnplannedVisitorRequestDto dto, string? photoUrl)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (string.IsNullOrWhiteSpace(dto.VisitorName))
        {
            throw new InvalidOperationException("Visitor name is required.");
        }

        var (securityUser, societyId) = await GetSecurityUserAndSocietyIdAsync(currentUserId);

        var flatConfig = await _context.PmSocietyWingFlatConfigs
            .AsNoTracking()
            .Include(c => c.Wing)
            .Include(c => c.Flat)
            .FirstOrDefaultAsync(c => c.Id == dto.SocietyWingFlatConfigId && c.SocietyId == societyId);

        if (flatConfig == null)
        {
            throw new InvalidOperationException("Selected flat configuration is invalid or does not belong to your society.");
        }

        // Primary resident resolution
        var residentMappings = await _context.ResidentFlatMappings
            .AsNoTracking()
            .Include(m => m.Resident)
            .Where(m => m.SocietyWingFlatConfigId == dto.SocietyWingFlatConfigId && m.IsActive)
            .ToListAsync();

        Resident? targetResident = null;
        var primaryMapping = residentMappings.FirstOrDefault(m => m.IsPrimary);
        if (primaryMapping != null)
        {
            targetResident = primaryMapping.Resident;
        }
        else if (residentMappings.Count == 1)
        {
            targetResident = residentMappings[0].Resident;
        }

        if (targetResident == null)
        {
            throw new InvalidOperationException("Unable to uniquely resolve resident for this flat. Please verify flat mappings.");
        }

        var pendingStatus = await GetStatusByCodeAsync(VisitorStatusConstants.Pending);
        var wingName = flatConfig.Wing?.Name ?? string.Empty;
        var flatCode = flatConfig.Flat?.Code ?? string.Empty;
        int.TryParse(flatCode, out int flatNo);

        var request = new VisitorRequest
        {
            VisitorName = dto.VisitorName.Trim(),
            VisitorPhone = string.IsNullOrWhiteSpace(dto.VisitorPhone) ? null : dto.VisitorPhone.Trim(),
            Purpose = string.IsNullOrWhiteSpace(dto.Purpose) ? null : dto.Purpose.Trim(),
            Wing = wingName,
            FlatNo = flatNo,
            ResidentId = targetResident.Id,
            SecurityUserId = securityUser.Id,
            SocietyId = societyId,
            SocietyWingFlatConfigId = dto.SocietyWingFlatConfigId,
            VisitType = "UNPLANNED",
            ExpectedArrivalDateTime = DateTime.UtcNow,
            VisitorPhotoUrl = photoUrl,
            StatusId = pendingStatus.Id,
            CreatedDate = DateTime.UtcNow
        };

        await _context.VisitorRequests.AddAsync(request);
        await _context.SaveChangesAsync();

        await _notificationService.CreateUnplannedVisitorNotification(request);

        return await MapToResponseDtoAsync(request);
    }

    public async Task<VisitorRequestResponseDto?> ApproveUnplannedAsync(int currentUserId, int requestId)
    {
        var resident = await GetResidentBySysmUserIdAsync(currentUserId);
        if (resident == null)
        {
            throw new InvalidOperationException("Authenticated user is not a resident.");
        }

        var request = await _context.VisitorRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ResidentId == resident.Id);

        if (request == null)
        {
            return null;
        }

        var pendingStatus = await GetStatusByCodeAsync(VisitorStatusConstants.Pending);
        if (request.StatusId != pendingStatus.Id)
        {
            throw new InvalidOperationException("Only pending visitor requests can be approved.");
        }

        var approvedStatus = await GetStatusByCodeAsync(VisitorStatusConstants.Approved);
        request.StatusId = approvedStatus.Id;
        request.RespondedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _notificationService.CreateUnplannedVisitorApprovedNotification(request);

        return await MapToResponseDtoAsync(request);
    }

    public async Task<VisitorRequestResponseDto?> RejectUnplannedAsync(int currentUserId, int requestId)
    {
        var resident = await GetResidentBySysmUserIdAsync(currentUserId);
        if (resident == null)
        {
            throw new InvalidOperationException("Authenticated user is not a resident.");
        }

        var request = await _context.VisitorRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ResidentId == resident.Id);

        if (request == null)
        {
            return null;
        }

        var pendingStatus = await GetStatusByCodeAsync(VisitorStatusConstants.Pending);
        if (request.StatusId != pendingStatus.Id)
        {
            throw new InvalidOperationException("Only pending visitor requests can be rejected.");
        }

        var rejectedStatus = await GetStatusByCodeAsync(VisitorStatusConstants.Rejected);
        request.StatusId = rejectedStatus.Id;
        request.RespondedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _notificationService.CreateUnplannedVisitorRejectedNotification(request);

        return await MapToResponseDtoAsync(request);
    }

    public async Task<VisitorRequestResponseDto?> CancelPlannedAsync(int currentUserId, int requestId)
    {
        var resident = await GetResidentBySysmUserIdAsync(currentUserId);
        if (resident == null)
        {
            throw new InvalidOperationException("Authenticated user is not a resident.");
        }

        var request = await _context.VisitorRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ResidentId == resident.Id);

        if (request == null)
        {
            return null;
        }

        var checkedInStatus = await GetStatusByCodeAsync(VisitorStatusConstants.CheckedIn);
        var checkedOutStatus = await GetStatusByCodeAsync(VisitorStatusConstants.CheckedOut);

        if (request.StatusId == checkedInStatus.Id || request.StatusId == checkedOutStatus.Id)
        {
            throw new InvalidOperationException("Cannot cancel a visitor request that has already entered or completed the visit.");
        }

        var cancelledStatus = await GetStatusByCodeAsync(VisitorStatusConstants.Cancelled);
        request.StatusId = cancelledStatus.Id;

        await _context.SaveChangesAsync();

        return await MapToResponseDtoAsync(request);
    }

    public async Task<bool> VerifyOtpAsync(int currentUserId, VerifyOtpDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Otp))
        {
            throw new InvalidOperationException("OTP is required.");
        }

        var (securityUser, societyId) = await GetSecurityUserAndSocietyIdAsync(currentUserId);

        var request = await _context.VisitorRequests
            .FirstOrDefaultAsync(r => r.Id == dto.VisitorRequestId && r.SocietyId == societyId);

        if (request == null)
        {
            throw new InvalidOperationException("Visitor request not found for your society.");
        }

        if (request.VisitType != "PLANNED")
        {
            throw new InvalidOperationException("OTP verification is only valid for PLANNED visitor passes.");
        }

        var approvedStatus = await GetStatusByCodeAsync(VisitorStatusConstants.Approved);
        if (request.StatusId != approvedStatus.Id)
        {
            throw new InvalidOperationException("Visitor pass is not in APPROVED state.");
        }

        if (request.OTPVerifiedAt.HasValue)
        {
            throw new InvalidOperationException("OTP has already been verified and single-used.");
        }

        if (request.OTPExpiresAt.HasValue && request.OTPExpiresAt.Value < DateTime.UtcNow)
        {
            var expiredStatus = await GetStatusByCodeAsync(VisitorStatusConstants.Expired);
            request.StatusId = expiredStatus.Id;
            await _context.SaveChangesAsync();
            throw new InvalidOperationException("OTP has expired.");
        }

        if (string.IsNullOrEmpty(request.OTPHash))
        {
            throw new InvalidOperationException("No OTP hash exists for this request.");
        }

        bool isValid = false;
        try
        {
            isValid = BCrypt.Net.BCrypt.Verify(dto.Otp.Trim(), request.OTPHash);
        }
        catch
        {
            isValid = false;
        }

        if (!isValid)
        {
            throw new InvalidOperationException("Invalid OTP entered.");
        }

        request.OTPVerifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<VisitorVisitResponseDto> CheckInAsync(int currentUserId, CheckInDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var (securityUser, societyId) = await GetSecurityUserAndSocietyIdAsync(currentUserId);

        var request = await _context.VisitorRequests
            .Include(r => r.SocietyWingFlatConfig)
                .ThenInclude(c => c.Wing)
            .Include(r => r.SocietyWingFlatConfig)
                .ThenInclude(c => c.Flat)
            .FirstOrDefaultAsync(r => r.Id == dto.VisitorRequestId && r.SocietyId == societyId);

        if (request == null)
        {
            throw new InvalidOperationException("Visitor request not found for your society.");
        }

        var approvedStatus = await GetStatusByCodeAsync(VisitorStatusConstants.Approved);
        var checkedInStatus = await GetStatusByCodeAsync(VisitorStatusConstants.CheckedIn);

        if (request.VisitType == "PLANNED")
        {
            if (!request.OTPVerifiedAt.HasValue)
            {
                throw new InvalidOperationException("OTP must be verified prior to check-in for planned visitors.");
            }
        }
        else if (request.VisitType == "UNPLANNED")
        {
            if (request.StatusId != approvedStatus.Id)
            {
                throw new InvalidOperationException("Unplanned visitor request must be APPROVED by resident before check-in.");
            }
        }

        // Verify no active visit exists
        var activeVisitExists = await _context.VisitorVisits
            .AnyAsync(v => v.VisitorRequestId == request.Id && v.CheckInDateTime != null && v.CheckOutDateTime == null);

        if (activeVisitExists)
        {
            throw new InvalidOperationException("Visitor is already checked in.");
        }

        var visit = new VisitorVisit
        {
            VisitorRequestId = request.Id,
            CheckInDateTime = DateTime.UtcNow,
            CheckInSecurityUserId = securityUser.Id,
            Gate = dto.Gate ?? "Main Gate",
            StatusId = checkedInStatus.Id,
            CreatedDate = DateTime.UtcNow
        };

        request.StatusId = checkedInStatus.Id;
        request.SecurityUserId = securityUser.Id;
        request.AcknowledgedDate = DateTime.UtcNow;

        await _context.VisitorVisits.AddAsync(visit);
        await _context.SaveChangesAsync();

        await _notificationService.CreateVisitorCheckedInNotification(request);

        var wing = request.Wing ?? request.SocietyWingFlatConfig?.Wing?.Name;
        var flat = request.FlatNo?.ToString() ?? request.SocietyWingFlatConfig?.Flat?.Code;

        return new VisitorVisitResponseDto
        {
            Id = visit.Id,
            VisitorRequestId = request.Id,
            VisitorName = request.VisitorName,
            Wing = wing,
            FlatNo = flat,
            CheckInDateTime = visit.CheckInDateTime,
            CheckOutDateTime = null,
            CheckInSecurityUserId = securityUser.Id,
            CheckInSecurityUserName = securityUser.UserName ?? securityUser.Email,
            Gate = visit.Gate,
            StatusId = checkedInStatus.Id,
            StatusCode = checkedInStatus.Code,
            StatusName = checkedInStatus.Name,
            CreatedDate = visit.CreatedDate
        };
    }

    public async Task<VisitorVisitResponseDto> CheckOutAsync(int currentUserId, CheckOutDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var (securityUser, societyId) = await GetSecurityUserAndSocietyIdAsync(currentUserId);

        var request = await _context.VisitorRequests
            .Include(r => r.SocietyWingFlatConfig)
                .ThenInclude(c => c.Wing)
            .Include(r => r.SocietyWingFlatConfig)
                .ThenInclude(c => c.Flat)
            .FirstOrDefaultAsync(r => r.Id == dto.VisitorRequestId && r.SocietyId == societyId);

        if (request == null)
        {
            throw new InvalidOperationException("Visitor request not found for your society.");
        }

        var activeVisit = await _context.VisitorVisits
            .FirstOrDefaultAsync(v => v.VisitorRequestId == request.Id && v.CheckInDateTime != null && v.CheckOutDateTime == null);

        if (activeVisit == null)
        {
            throw new InvalidOperationException("No active check-in session found for this visitor.");
        }

        var checkedOutStatus = await GetStatusByCodeAsync(VisitorStatusConstants.CheckedOut);

        activeVisit.CheckOutDateTime = DateTime.UtcNow;
        activeVisit.CheckOutSecurityUserId = securityUser.Id;
        activeVisit.StatusId = checkedOutStatus.Id;

        request.StatusId = checkedOutStatus.Id;

        await _context.SaveChangesAsync();

        await _notificationService.CreateVisitorCheckedOutNotification(request);

        var wing = request.Wing ?? request.SocietyWingFlatConfig?.Wing?.Name;
        var flat = request.FlatNo?.ToString() ?? request.SocietyWingFlatConfig?.Flat?.Code;

        return new VisitorVisitResponseDto
        {
            Id = activeVisit.Id,
            VisitorRequestId = request.Id,
            VisitorName = request.VisitorName,
            Wing = wing,
            FlatNo = flat,
            CheckInDateTime = activeVisit.CheckInDateTime,
            CheckOutDateTime = activeVisit.CheckOutDateTime,
            CheckInSecurityUserId = activeVisit.CheckInSecurityUserId,
            CheckOutSecurityUserId = securityUser.Id,
            CheckOutSecurityUserName = securityUser.UserName ?? securityUser.Email,
            Gate = activeVisit.Gate,
            StatusId = checkedOutStatus.Id,
            StatusCode = checkedOutStatus.Code,
            StatusName = checkedOutStatus.Name,
            CreatedDate = activeVisit.CreatedDate
        };
    }

    public async Task<List<VisitorRequestResponseDto>> GetResidentRequestsAsync(int currentUserId)
    {
        var resident = await GetResidentBySysmUserIdAsync(currentUserId);
        if (resident == null)
        {
            return new List<VisitorRequestResponseDto>();
        }

        var requests = await _context.VisitorRequests
            .AsNoTracking()
            .Include(r => r.Resident)
            .Include(r => r.SecurityUser)
            .Include(r => r.Society)
            .Include(r => r.SocietyWingFlatConfig)
                .ThenInclude(c => c.Wing)
            .Include(r => r.SocietyWingFlatConfig)
                .ThenInclude(c => c.Flat)
            .Include(r => r.Status)
            .Include(r => r.VisitorVisits)
            .Where(r => r.ResidentId == resident.Id)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

        return MapListToDto(requests);
    }

    public async Task<List<VisitorRequestResponseDto>> GetGateRequestsAsync(int currentUserId)
    {
        var (_, societyId) = await GetSecurityUserAndSocietyIdAsync(currentUserId);

        var pendingStatus = await GetStatusByCodeAsync(VisitorStatusConstants.Pending);
        var approvedStatus = await GetStatusByCodeAsync(VisitorStatusConstants.Approved);

        var requests = await _context.VisitorRequests
            .AsNoTracking()
            .Include(r => r.Resident)
            .Include(r => r.SecurityUser)
            .Include(r => r.Society)
            .Include(r => r.SocietyWingFlatConfig)
                .ThenInclude(c => c.Wing)
            .Include(r => r.SocietyWingFlatConfig)
                .ThenInclude(c => c.Flat)
            .Include(r => r.Status)
            .Include(r => r.VisitorVisits)
            .Where(r => r.SocietyId == societyId &&
                        (r.StatusId == pendingStatus.Id || r.StatusId == approvedStatus.Id))
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

        return MapListToDto(requests);
    }

    public async Task<List<VisitorRequestResponseDto>> GetCurrentlyInsideAsync(int currentUserId)
    {
        int? societyId = await DeriveSocietyIdForUserAsync(currentUserId);

        var activeVisits = await _context.VisitorVisits
            .AsNoTracking()
            .Include(v => v.VisitorRequest)
                .ThenInclude(r => r.Resident)
            .Include(v => v.VisitorRequest)
                .ThenInclude(r => r.SecurityUser)
            .Include(v => v.VisitorRequest)
                .ThenInclude(r => r.Society)
            .Include(v => v.VisitorRequest)
                .ThenInclude(r => r.SocietyWingFlatConfig)
                    .ThenInclude(c => c.Wing)
            .Include(v => v.VisitorRequest)
                .ThenInclude(r => r.SocietyWingFlatConfig)
                    .ThenInclude(c => c.Flat)
            .Include(v => v.VisitorRequest)
                .ThenInclude(r => r.Status)
            .Where(v => v.CheckInDateTime != null &&
                        v.CheckOutDateTime == null &&
                        (!societyId.HasValue || v.VisitorRequest.SocietyId == societyId.Value))
            .OrderByDescending(v => v.CheckInDateTime)
            .ToListAsync();

        var requests = activeVisits.Select(v => v.VisitorRequest).Distinct().ToList();
        return MapListToDto(requests);
    }

    public async Task<List<VisitorRequestResponseDto>> GetSocietyHistoryAsync(int currentUserId)
    {
        int? societyId = await DeriveSocietyIdForUserAsync(currentUserId);

        var query = _context.VisitorRequests
            .AsNoTracking()
            .Include(r => r.Resident)
            .Include(r => r.SecurityUser)
            .Include(r => r.Society)
            .Include(r => r.SocietyWingFlatConfig)
                .ThenInclude(c => c.Wing)
            .Include(r => r.SocietyWingFlatConfig)
                .ThenInclude(c => c.Flat)
            .Include(r => r.Status)
            .Include(r => r.VisitorVisits)
            .AsQueryable();

        if (societyId.HasValue)
        {
            query = query.Where(r => r.SocietyId == societyId.Value);
        }

        var requests = await query
            .OrderByDescending(r => r.CreatedDate)
            .Take(200)
            .ToListAsync();

        return MapListToDto(requests);
    }

    public async Task<List<WingDto>> GetSocietyWingsAsync(int currentUserId)
    {
        var (_, societyId) = await GetSecurityUserAndSocietyIdAsync(currentUserId);

        var wings = await _context.PmSocietyWingFlatConfigs
            .AsNoTracking()
            .Where(c => c.SocietyId == societyId && c.IsActive && c.Wing.IsActive)
            .Select(c => c.Wing)
            .Distinct()
            .OrderBy(w => w.Name)
            .Select(w => new WingDto
            {
                Id = w.Id,
                Code = w.Code,
                Name = w.Name
            })
            .ToListAsync();

        return wings;
    }

    public async Task<List<FloorDto>> GetSocietyFloorsAsync(int currentUserId, int wingId)
    {
        if (wingId <= 0)
        {
            return new List<FloorDto>();
        }

        var (_, societyId) = await GetSecurityUserAndSocietyIdAsync(currentUserId);

        var floors = await _context.PmSocietyWingFlatConfigs
            .AsNoTracking()
            .Where(c => c.SocietyId == societyId && c.WingId == wingId && c.IsActive && c.Floor.IsActive)
            .Select(c => c.Floor)
            .Distinct()
            .OrderBy(f => f.FloorNumber)
            .Select(f => new FloorDto
            {
                Id = f.Id,
                Code = f.Code,
                Name = f.Name,
                FloorNumber = f.FloorNumber
            })
            .ToListAsync();

        return floors;
    }

    public async Task<List<FlatDto>> GetSocietyFlatsAsync(int currentUserId, int wingId, int floorId)
    {
        if (wingId <= 0 || floorId <= 0)
        {
            return new List<FlatDto>();
        }

        var (_, societyId) = await GetSecurityUserAndSocietyIdAsync(currentUserId);

        var flats = await _context.PmSocietyWingFlatConfigs
            .AsNoTracking()
            .Where(c => c.SocietyId == societyId && c.WingId == wingId && c.FloorId == floorId && c.IsActive)
            .Select(c => c.Flat)
            .Distinct()
            .OrderBy(fl => fl.Code)
            .Select(fl => new FlatDto
            {
                Id = fl.Id,
                Code = fl.Code
            })
            .ToListAsync();

        return flats;
    }

    public async Task<(object? Data, string? ErrorMessage)> LookupResidentAsync(int currentUserId, string? wing, int? flatNo)
    {
        if (string.IsNullOrWhiteSpace(wing) || !flatNo.HasValue || flatNo.Value <= 0)
        {
            return (null, "Wing and a valid flat number are required.");
        }

        var normalizedWing = wing.Trim().ToUpperInvariant();

        var societyId = await _context.SocietyUserMappings
            .AsNoTracking()
            .Where(x => x.UserId == currentUserId && x.IsActive)
            .Select(x => x.SocietyId)
            .FirstOrDefaultAsync();

        if (societyId == 0)
        {
            return (null, "Authenticated security user is not mapped to an active society.");
        }

        var residentMapping = await _context.ResidentFlatMappings
            .AsNoTracking()
            .Include(m => m.Resident)
            .Include(m => m.SocietyWingFlatConfig)
                .ThenInclude(c => c.Wing)
            .Include(m => m.SocietyWingFlatConfig)
                .ThenInclude(c => c.Flat)
            .Where(m => m.IsActive &&
                        m.SocietyWingFlatConfig.SocietyId == societyId &&
                        (m.SocietyWingFlatConfig.Wing.Code.ToUpper() == normalizedWing || m.SocietyWingFlatConfig.Wing.Name.ToUpper() == normalizedWing) &&
                        m.SocietyWingFlatConfig.Flat.Code == flatNo.ToString() &&
                        m.Resident.Status == "Approved")
            .FirstOrDefaultAsync();

        if (residentMapping?.Resident != null)
        {
            var res = residentMapping.Resident;
            return (new
            {
                res.Id,
                res.Name,
                Wing = normalizedWing,
                FlatNo = flatNo.Value,
                SocietyWingFlatConfigId = residentMapping.SocietyWingFlatConfigId,
                OwnershipType = residentMapping.OwnershipType
            }, null);
        }

        return (null, $"No approved resident found for Wing {normalizedWing} and Flat {flatNo} in your society.");
    }

    public async Task<(object? Data, string? ErrorMessage)> LookupResidentByFlatConfigAsync(int currentUserId, int wingId, int floorId, int flatId)
    {
        if (wingId <= 0 || floorId <= 0 || flatId <= 0)
        {
            return (null, "Wing, floor, and flat selections are required.");
        }

        var (_, societyId) = await GetSecurityUserAndSocietyIdAsync(currentUserId);

        var config = await _context.PmSocietyWingFlatConfigs
            .AsNoTracking()
            .Include(c => c.Wing)
            .Include(c => c.Flat)
            .FirstOrDefaultAsync(c => c.SocietyId == societyId && c.WingId == wingId && c.FloorId == floorId && c.FlatId == flatId && c.IsActive);

        if (config == null)
        {
            return (null, "No active flat configuration found for this unit in your society.");
        }

        var residentMappings = await _context.ResidentFlatMappings
            .AsNoTracking()
            .Include(m => m.Resident)
            .Where(m => m.SocietyWingFlatConfigId == config.Id && m.IsActive && m.Resident.Status == "Approved")
            .ToListAsync();

        if (residentMappings.Count == 0)
        {
            return (null, $"No approved resident found for Wing {config.Wing?.Name} Flat {config.Flat?.Code} in your society.");
        }

        var primaryMapping = residentMappings.FirstOrDefault(m => m.IsPrimary) ?? residentMappings.FirstOrDefault();
        var res = primaryMapping!.Resident;

        int.TryParse(config.Flat?.Code, out int flatNo);

        return (new
        {
            res.Id,
            res.Name,
            Wing = config.Wing?.Name,
            FlatNo = flatNo,
            SocietyWingFlatConfigId = config.Id,
            OwnershipType = primaryMapping.OwnershipType
        }, null);
    }

    // Helper Methods
    private async Task<VisitorStatus> GetStatusByCodeAsync(string code)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var status = await _context.VisitorStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code.ToUpper() == normalizedCode);

        if (status == null)
        {
            throw new InvalidOperationException($"Visitor status code '{code}' not found in database.");
        }

        return status;
    }

    private async Task<Resident?> GetResidentBySysmUserIdAsync(int sysmUserId)
    {
        return await _context.Residents
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == sysmUserId);
    }

    private async Task<(SysmUser User, int SocietyId)> GetSecurityUserAndSocietyIdAsync(int sysmUserId)
    {
        var user = await _context.SysmUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == sysmUserId && u.IsActive);

        if (user == null)
        {
            throw new InvalidOperationException("Authenticated security user not found or inactive.");
        }

        var mapping = await _context.SocietyUserMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == sysmUserId && m.IsActive);

        if (mapping == null)
        {
            throw new InvalidOperationException("Security user is not associated with any active society.");
        }

        return (user, mapping.SocietyId);
    }

    private async Task<int?> DeriveSocietyIdForUserAsync(int sysmUserId)
    {
        var user = await _context.SysmUsers
            .AsNoTracking()
            .Include(u => u.RoleNavigation)
            .FirstOrDefaultAsync(u => u.Id == sysmUserId);

        if (user == null)
        {
            return null;
        }

        var roleCode = user.RoleNavigation?.Code ?? user.Role;

        if (roleCode == AppRoles.SuperAdmin)
        {
            return null; // Global access
        }

        if (roleCode == AppRoles.SocietyAdmin || roleCode == AppRoles.Security)
        {
            var mapping = await _context.SocietyUserMappings
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == sysmUserId && m.IsActive);
            return mapping?.SocietyId;
        }

        if (roleCode == AppRoles.Resident)
        {
            var resident = await GetResidentBySysmUserIdAsync(sysmUserId);
            if (resident != null)
            {
                var flatMapping = await _context.ResidentFlatMappings
                    .AsNoTracking()
                    .Include(m => m.SocietyWingFlatConfig)
                    .FirstOrDefaultAsync(m => m.ResidentId == resident.Id && m.IsActive);
                return flatMapping?.SocietyWingFlatConfig?.SocietyId;
            }
        }

        return null;
    }

    private async Task<VisitorRequestResponseDto> MapToResponseDtoAsync(VisitorRequest request)
    {
        if (request.Status == null && request.StatusId.HasValue)
        {
            request.Status = await _context.VisitorStatuses.FindAsync(request.StatusId.Value);
        }

        if (request.Resident == null)
        {
            request.Resident = await _context.Residents.FindAsync(request.ResidentId) ?? new Resident();
        }

        if (request.Society == null && request.SocietyId.HasValue)
        {
            request.Society = await _context.SocietyMasters.FindAsync(request.SocietyId.Value);
        }

        var activeVisit = request.VisitorVisits?.OrderByDescending(v => v.CreatedDate).FirstOrDefault();

        var wingName = request.Wing ?? request.SocietyWingFlatConfig?.Wing?.Name;
        var flatCode = request.FlatNo?.ToString() ?? request.SocietyWingFlatConfig?.Flat?.Code;

        return new VisitorRequestResponseDto
        {
            Id = request.Id,
            VisitorName = request.VisitorName,
            VisitorPhone = request.VisitorPhone,
            Purpose = request.Purpose,
            Wing = wingName,
            FlatNo = flatCode,
            ResidentId = request.ResidentId,
            ResidentName = request.Resident?.Name ?? "Resident",
            SecurityUserId = request.SecurityUserId,
            SecurityUserName = request.SecurityUser?.UserName ?? request.SecurityUser?.Email,
            CreatedDate = request.CreatedDate,
            RespondedDate = request.RespondedDate,
            AcknowledgedDate = request.AcknowledgedDate,
            VisitorPhotoUrl = request.VisitorPhotoUrl,
            SocietyId = request.SocietyId,
            SocietyName = request.Society?.Name,
            SocietyWingFlatConfigId = request.SocietyWingFlatConfigId,
            VisitType = request.VisitType,
            ExpectedArrivalDateTime = request.ExpectedArrivalDateTime,
            OTPExpiresAt = request.OTPExpiresAt,
            OTPVerifiedAt = request.OTPVerifiedAt,
            StatusId = request.StatusId,
            StatusCode = request.Status?.Code,
            StatusName = request.Status?.Name,
            CheckInDateTime = activeVisit?.CheckInDateTime,
            CheckOutDateTime = activeVisit?.CheckOutDateTime
        };
    }

    private List<VisitorRequestResponseDto> MapListToDto(List<VisitorRequest> requests)
    {
        return requests.Select(request =>
        {
            var activeVisit = request.VisitorVisits?.OrderByDescending(v => v.CreatedDate).FirstOrDefault();
            var wingName = request.Wing ?? request.SocietyWingFlatConfig?.Wing?.Name;
            var flatCode = request.FlatNo?.ToString() ?? request.SocietyWingFlatConfig?.Flat?.Code;

            return new VisitorRequestResponseDto
            {
                Id = request.Id,
                VisitorName = request.VisitorName,
                VisitorPhone = request.VisitorPhone,
                Purpose = request.Purpose,
                Wing = wingName,
                FlatNo = flatCode,
                ResidentId = request.ResidentId,
                ResidentName = request.Resident?.Name ?? "Resident",
                SecurityUserId = request.SecurityUserId,
                SecurityUserName = request.SecurityUser?.UserName ?? request.SecurityUser?.Email,
                CreatedDate = request.CreatedDate,
                RespondedDate = request.RespondedDate,
                AcknowledgedDate = request.AcknowledgedDate,
                VisitorPhotoUrl = request.VisitorPhotoUrl,
                SocietyId = request.SocietyId,
                SocietyName = request.Society?.Name,
                SocietyWingFlatConfigId = request.SocietyWingFlatConfigId,
                VisitType = request.VisitType,
                ExpectedArrivalDateTime = request.ExpectedArrivalDateTime,
                OTPExpiresAt = request.OTPExpiresAt,
                OTPVerifiedAt = request.OTPVerifiedAt,
                StatusId = request.StatusId,
                StatusCode = request.Status?.Code,
                StatusName = request.Status?.Name,
                CheckInDateTime = activeVisit?.CheckInDateTime,
                CheckOutDateTime = activeVisit?.CheckOutDateTime
            };
        }).ToList();
    }
}
