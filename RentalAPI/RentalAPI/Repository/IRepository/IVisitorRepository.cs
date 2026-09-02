using System.Collections.Generic;
using System.Threading.Tasks;
using RentalAPI.DTO;

namespace RentalAPI.Repository.IRepository;

public interface IVisitorRepository
{
    Task<PlannedVisitorResponseDto> CreatePlannedAsync(int currentUserId, CreatePlannedVisitorRequestDto dto);

    Task<VisitorRequestResponseDto> CreateUnplannedAsync(int currentUserId, CreateUnplannedVisitorRequestDto dto, string? photoUrl);

    Task<VisitorRequestResponseDto?> ApproveUnplannedAsync(int currentUserId, int requestId);

    Task<VisitorRequestResponseDto?> RejectUnplannedAsync(int currentUserId, int requestId);

    Task<VisitorRequestResponseDto?> CancelPlannedAsync(int currentUserId, int requestId);

    Task<bool> VerifyOtpAsync(int currentUserId, VerifyOtpDto dto);

    Task<VisitorVisitResponseDto> CheckInAsync(int currentUserId, CheckInDto dto);

    Task<VisitorVisitResponseDto> CheckOutAsync(int currentUserId, CheckOutDto dto);

    Task<List<VisitorRequestResponseDto>> GetResidentRequestsAsync(int currentUserId);

    Task<List<VisitorRequestResponseDto>> GetGateRequestsAsync(int currentUserId);

    Task<List<VisitorRequestResponseDto>> GetCurrentlyInsideAsync(int currentUserId);

    Task<List<VisitorRequestResponseDto>> GetSocietyHistoryAsync(int currentUserId);

    Task<List<WingDto>> GetSocietyWingsAsync(int currentUserId);

    Task<List<FloorDto>> GetSocietyFloorsAsync(int currentUserId, int wingId);

    Task<List<FlatDto>> GetSocietyFlatsAsync(int currentUserId, int wingId, int floorId);

    Task<(object? Data, string? ErrorMessage)> LookupResidentAsync(int currentUserId, string? wing, int? flatNo);

    Task<(object? Data, string? ErrorMessage)> LookupResidentByFlatConfigAsync(int currentUserId, int wingId, int floorId, int flatId);
}
