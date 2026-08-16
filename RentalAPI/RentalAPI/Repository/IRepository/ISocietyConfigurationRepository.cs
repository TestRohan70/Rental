using RentalAPI.DTO.PAdmin;

namespace RentalAPI.Repository.IRepository;

public interface ISocietyConfigurationRepository
{
    Task<List<SocietyResponseDto>> GetSocietiesAsync(string? search, CancellationToken cancellationToken = default);

    Task<SocietyResponseDto?> GetSocietyAsync(int societyId, CancellationToken cancellationToken = default);

    Task<SocietyResponseDto> CreateSocietyAsync(CreateSocietyDto dto, CancellationToken cancellationToken = default);

    Task<SocietyResponseDto?> UpdateSocietyAsync(int societyId, UpdateSocietyDto dto, CancellationToken cancellationToken = default);

    Task DeleteSocietyAsync(int societyId, CancellationToken cancellationToken = default);

    Task<SocietyStructureDto?> GetSocietyStructureAsync(int societyId, CancellationToken cancellationToken = default);

    Task<List<WingResponseDto>> GetActiveWingsAsync(CancellationToken cancellationToken = default);

    Task<List<FloorResponseDto>> GetActiveFloorsAsync(int? wingId, CancellationToken cancellationToken = default);

    Task<List<FlatResponseDto>> GetActiveFlatsAsync(CancellationToken cancellationToken = default);

    Task<FlatResponseDto> AddMappingAsync(int societyId, CreateSocietyMappingDto dto, CancellationToken cancellationToken = default);

    Task DeactivateWingForSocietyAsync(int societyId, int wingId, CancellationToken cancellationToken = default);

    Task DeactivateFloorForSocietyAsync(int societyId, int wingId, int floorId, CancellationToken cancellationToken = default);

    Task DeactivateFlatAsync(int societyId, int wingId, int floorId, int flatId, CancellationToken cancellationToken = default);

    Task<GenerateStructurePreviewDto> GenerateStructureAsync(int societyId, GenerateSocietyStructureDto dto, CancellationToken cancellationToken = default);
}
