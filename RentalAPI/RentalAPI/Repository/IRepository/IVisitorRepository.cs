using RentalAPI.DTO;

namespace RentalAPI.Repository.IRepository;

public interface IVisitorRepository
{
    Task<VisitorRequestDto> CreateAsync(CreateVisitorRequestDto dto);

    Task<List<VisitorRequestDto>> GetGateRequestsAsync(int securityId);

    Task<List<VisitorRequestDto>> GetResidentRequestsAsync(int residentId);

    Task<VisitorRequestDto?> ApproveAsync(int requestId, int residentId);

    Task<VisitorRequestDto?> RejectAsync(int requestId, int residentId);

    Task<VisitorRequestDto?> AcknowledgeAsync(int requestId, int securityId);

    Task<(object? Data, string? ErrorMessage)> LookupResidentAsync(string wing, int flatNo);
}
