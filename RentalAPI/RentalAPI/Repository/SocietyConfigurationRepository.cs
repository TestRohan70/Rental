using Microsoft.EntityFrameworkCore;
using RentalAPI.DTO;
using RentalAPI.DTO.PAdmin;
using RentalAPI.Models;
using RentalAPI.Repository.IRepository;

namespace RentalAPI.Repository;

public class SocietyConfigurationRepository : ISocietyConfigurationRepository
{
    private const int MaxBulkMappings = 5000;

    private readonly AppDbContext _context;

    public SocietyConfigurationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<SocietyResponseDto>> GetSocietiesAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.SocietyMasters.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term) || x.Location != null && x.Location.Contains(term));
        }

        var societies = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var configs = await _context.PmSocietyWingFlatConfigs
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        return societies.Select(s => MapSocietySummary(s, configs.Where(c => c.SocietyId == s.Id))).ToList();
    }

    public async Task<SocietyResponseDto?> GetSocietyAsync(int societyId, CancellationToken cancellationToken = default)
    {
        var society = await _context.SocietyMasters.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == societyId, cancellationToken);

        if (society is null)
        {
            return null;
        }

        var configs = await GetActiveConfigsForSocietyAsync(societyId, cancellationToken);
        return MapSocietySummary(society, configs);
    }

    public async Task<SocietyResponseDto> CreateSocietyAsync(CreateSocietyDto dto, CancellationToken cancellationToken = default)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Society name is required.");
        }

        var duplicate = await _context.SocietyMasters
            .AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("A society with this name already exists.");
        }

        var society = new SocietyMaster
        {
            Code = await GenerateSocietyCodeAsync(cancellationToken),
            Name = name,
            Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim()
        };

        await _context.SocietyMasters.AddAsync(society, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapSocietySummary(society, []);
    }

    public async Task<SocietyResponseDto?> UpdateSocietyAsync(int societyId, UpdateSocietyDto dto, CancellationToken cancellationToken = default)
    {
        var society = await _context.SocietyMasters.FirstOrDefaultAsync(x => x.Id == societyId, cancellationToken);
        if (society is null)
        {
            return null;
        }

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Society name is required.");
        }

        var duplicate = await _context.SocietyMasters
            .AnyAsync(x => x.Id != societyId && x.Name.ToLower() == name.ToLower(), cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("A society with this name already exists.");
        }

        society.Name = name;
        society.Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
        await _context.SaveChangesAsync(cancellationToken);

        var configs = await GetActiveConfigsForSocietyAsync(societyId, cancellationToken);
        return MapSocietySummary(society, configs);
    }

    public async Task DeleteSocietyAsync(int societyId, CancellationToken cancellationToken = default)
    {
        var society = await _context.SocietyMasters.FirstOrDefaultAsync(x => x.Id == societyId, cancellationToken)
            ?? throw new InvalidOperationException("Society not found.");

        var hasActiveConfig = await _context.PmSocietyWingFlatConfigs
            .AnyAsync(x => x.SocietyId == societyId && x.IsActive, cancellationToken);

        if (hasActiveConfig)
        {
            throw new InvalidOperationException("Society cannot be deleted because it has active wing/floor/flat configuration.");
        }

        var hasResidents = await _context.ResidentFlatMappings
            .AnyAsync(x => x.SocietyWingFlatConfig.SocietyId == societyId && x.IsActive, cancellationToken);

        if (hasResidents)
        {
            throw new InvalidOperationException("Society cannot be deleted because residents are linked to it.");
        }

        _context.SocietyMasters.Remove(society);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SocietyStructureDto?> GetSocietyStructureAsync(int societyId, CancellationToken cancellationToken = default)
    {
        var society = await GetSocietyAsync(societyId, cancellationToken);
        if (society is null)
        {
            return null;
        }

        var configs = await _context.PmSocietyWingFlatConfigs
            .AsNoTracking()
            .Include(x => x.Wing)
            .Include(x => x.Floor)
            .Include(x => x.FlatType)
            .Where(x => x.SocietyId == societyId && x.IsActive)
            .ToListAsync(cancellationToken);

        return new SocietyStructureDto
        {
            Society = society,
            Wings = BuildStructureTree(configs)
        };
    }

    public async Task<List<SocietyFlatConfigResponseDto>> GetSocietyFlatsAsync(
        int societyId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PmSocietyWingFlatConfigs
            .AsNoTracking()
            .Where(config => config.SocietyId == societyId && config.IsActive)
            .OrderBy(config => config.WingId)
            .ThenBy(config => config.FloorId)
            .ThenBy(config => config.FlatNumber)
            .Select(config => new SocietyFlatConfigResponseDto
            {
                Id = config.Id,
                FlatId = config.FlatID,
                SocietyId = config.SocietyId,
                WingId = config.WingId,
                WingCode = config.Wing.Code,
                WingName = config.Wing.Name,
                FloorId = config.FloorId,
                FloorName = config.Floor.Name,
                FloorNumber = config.Floor.FloorNumber,
                Code = config.FlatNumber ?? string.Empty,
                TypeId = config.FlatTypeId,
                TypeName = config.FlatType.Type,
                IsActive = config.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WingResponseDto>> GetActiveWingsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WingMasters
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new WingResponseDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FloorResponseDto>> GetActiveFloorsAsync(int societyId, CancellationToken cancellationToken = default)
    {
        return await _context.FloorMasters
            .AsNoTracking()
            .Where(x => x.IsActive && x.SocietyID == societyId)
            .OrderBy(x => x.FloorNumber)
            .Select(x => new FloorResponseDto
            {
                Id = x.Id,
                SocietyID = x.SocietyID,
                WingID = x.WingID,
                Code = x.Code,
                Name = x.Name,
                FloorNumber = x.FloorNumber,
                IsActive = x.IsActive,
                FlatCount = _context.PmSocietyWingFlatConfigs.Count(config =>
                    config.SocietyId == societyId &&
                    config.WingId == x.WingID &&
                    config.FloorId == x.Id &&
                    config.IsActive)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FlatResponseDto>> GetActiveFlatsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FlatMasters
            .AsNoTracking()
            .Include(x => x.Type)
            .OrderBy(x => x.Code)
            .Select(x => new FlatResponseDto
            {
                Id = x.Id,
                Code = x.Code,
                TypeId = x.TypeId,
                TypeName = x.Type != null ? x.Type.Type : null,
                IsActive = true
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<FlatResponseDto> AddMappingAsync(int societyId, CreateSocietyMappingDto dto, CancellationToken cancellationToken = default)
    {
        var flatTypeId = dto.ResolvedFlatTypeId;
        if (flatTypeId <= 0)
        {
            throw new InvalidOperationException("Flat Type is required.");
        }

        await EnsureSocietyExistsAsync(societyId, cancellationToken);
        await ValidateMasterReferencesAsync(dto.WingId, dto.FloorId, flatTypeId, cancellationToken);
        await ValidateWingFlatUniquenessAsync(societyId, dto.WingId, dto.FloorId, flatTypeId, cancellationToken);

        var config = await UpsertMappingAsync(societyId, dto.WingId, dto.FloorId, flatTypeId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapConfigToFlatDto(config);
    }

    public async Task DeactivateWingForSocietyAsync(int societyId, int wingId, CancellationToken cancellationToken = default)
    {
        var configs = await _context.PmSocietyWingFlatConfigs
            .Where(x => x.SocietyId == societyId && x.WingId == wingId && x.IsActive)
            .ToListAsync(cancellationToken);

        if (configs.Count == 0)
        {
            throw new InvalidOperationException("Wing is not configured for this society.");
        }

        foreach (var config in configs)
        {
            config.IsActive = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateFloorForSocietyAsync(int societyId, int wingId, int floorId, CancellationToken cancellationToken = default)
    {
        var configs = await _context.PmSocietyWingFlatConfigs
            .Where(x => x.SocietyId == societyId && x.WingId == wingId && x.FloorId == floorId && x.IsActive)
            .ToListAsync(cancellationToken);

        if (configs.Count == 0)
        {
            throw new InvalidOperationException("Floor is not configured for this society wing.");
        }

        foreach (var config in configs)
        {
            config.IsActive = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateFlatAsync(int societyId, int wingId, int floorId, int flatId, CancellationToken cancellationToken = default)
    {
        var config = await _context.PmSocietyWingFlatConfigs
            .FirstOrDefaultAsync(x =>
                x.SocietyId == societyId &&
                x.WingId == wingId &&
                x.FloorId == floorId &&
                (x.Id == flatId || x.FlatTypeId == flatId) &&
                x.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException("Flat configuration not found.");

        config.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<GenerateStructurePreviewDto> GenerateStructureAsync(int societyId, GenerateSocietyStructureDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureSocietyExistsAsync(societyId, cancellationToken);

        var wingIds = dto.WingIds.Distinct().ToList();
        var floorIds = dto.FloorIds.Distinct().ToList();
        var flatIds = dto.FlatIds.Distinct().ToList();

        if (wingIds.Count == 0 || floorIds.Count == 0 || flatIds.Count == 0)
        {
            throw new InvalidOperationException("Select at least one wing, floor, and flat.");
        }

        var totalMappings = wingIds.Count * floorIds.Count * flatIds.Count;
        if (totalMappings > MaxBulkMappings)
        {
            throw new InvalidOperationException($"Bulk generation is limited to {MaxBulkMappings} mappings per request.");
        }

        var wings = await _context.WingMasters.AsNoTracking()
            .Where(x => wingIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);

        var floors = await _context.FloorMasters.AsNoTracking()
            .Where(x => floorIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);

        var flats = await _context.FlatMasters.AsNoTracking()
            .Include(x => x.Type)
            .Where(x => flatIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        ValidateSelectedMasters(wingIds, floorIds, flatIds, wings, floors, flats);

        var existingActive = await _context.PmSocietyWingFlatConfigs
            .AsNoTracking()
            .Where(x => x.SocietyId == societyId && x.IsActive)
            .Select(x => new { x.WingId, x.FloorId, x.FlatTypeId })
            .ToListAsync(cancellationToken);

        var existingExact = existingActive
            .Select(x => ToExactMappingKey(x.WingId, x.FloorId, x.FlatTypeId))
            .ToHashSet(StringComparer.Ordinal);

        var existingWingFlat = existingActive
            .Select(x => ToWingFlatKey(x.WingId, x.FlatTypeId))
            .ToHashSet(StringComparer.Ordinal);

        var (eligibleMappings, skippedDuplicates) = BuildEligibleMappings(
            wings,
            floors,
            flats,
            wingIds,
            floorIds,
            flatIds,
            existingExact,
            existingWingFlat);

        if (dto.PreviewOnly)
        {
            return new GenerateStructurePreviewDto
            {
                TotalWings = wings.Count,
                TotalFloors = floors.Count,
                TotalFlats = flats.Count,
                TotalMappings = eligibleMappings.Count,
                SkippedDuplicates = skippedDuplicates,
                Preview = BuildStructureTree(ToPreviewConfigs(eligibleMappings))
            };
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var created = 0;

        try
        {
            foreach (var mapping in eligibleMappings)
            {
                await UpsertMappingAsync(
                    societyId,
                    mapping.WingId,
                    mapping.FloorId,
                    mapping.FlatId,
                    cancellationToken);
                created++;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (created == 0)
            {
                throw new InvalidOperationException("All selected mappings already exist or duplicate flats within the same wing.");
            }
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var structure = await GetSocietyStructureAsync(societyId, cancellationToken);

        return new GenerateStructurePreviewDto
        {
            TotalWings = wings.Count,
            TotalFloors = floors.Count,
            TotalFlats = flats.Count,
            TotalMappings = created,
            SkippedDuplicates = skippedDuplicates,
            Preview = structure?.Wings ?? []
        };
    }

    private sealed record EligibleMapping(int WingId, int FloorId, int FlatId, WingMaster Wing, FloorMaster Floor, FlatMaster Flat);

    private static (List<EligibleMapping> Eligible, int Skipped) BuildEligibleMappings(
        List<WingMaster> wings,
        List<FloorMaster> floors,
        List<FlatMaster> flats,
        List<int> wingIds,
        List<int> floorIds,
        List<int> flatIds,
        HashSet<string> existingExact,
        HashSet<string> existingWingFlat)
    {
        var wingMap = wings.ToDictionary(x => x.Id);
        var floorMap = floors.ToDictionary(x => x.Id);
        var flatMap = flats.ToDictionary(x => x.Id);
        var eligible = new List<EligibleMapping>();
        var batchWingFlat = new HashSet<string>(StringComparer.Ordinal);
        var skipped = 0;

        foreach (var wingId in wingIds)
        {
            foreach (var floorId in floorIds)
            {
                foreach (var flatId in flatIds)
                {
                    var exactKey = ToExactMappingKey(wingId, floorId, flatId);
                    var wingFlatKey = ToWingFlatKey(wingId, flatId);

                    if (existingExact.Contains(exactKey) ||
                        existingWingFlat.Contains(wingFlatKey) ||
                        batchWingFlat.Contains(wingFlatKey))
                    {
                        skipped++;
                        continue;
                    }

                    batchWingFlat.Add(wingFlatKey);
                    eligible.Add(new EligibleMapping(
                        wingId,
                        floorId,
                        flatId,
                        wingMap[wingId],
                        floorMap[floorId],
                        flatMap[flatId]));
                }
            }
        }

        return (eligible, skipped);
    }

    private static List<PmSocietyWingFlatConfig> ToPreviewConfigs(List<EligibleMapping> mappings) =>
        mappings.Select(m => new PmSocietyWingFlatConfig
        {
            WingId = m.WingId,
            FloorId = m.FloorId,
            FlatTypeId = m.Flat.TypeId ?? m.FlatId,
            FlatNumber = m.Flat.Code,
            IsActive = true,
            Wing = m.Wing,
            Floor = m.Floor,
            FlatType = m.Flat.Type!
        }).ToList();

    private static string ToExactMappingKey(int wingId, int floorId, int flatId) =>
        $"{wingId}:{floorId}:{flatId}";

    private static string ToWingFlatKey(int wingId, int flatId) =>
        $"{wingId}:{flatId}";

    private static void ValidateSelectedMasters(
        List<int> wingIds,
        List<int> floorIds,
        List<int> flatIds,
        List<WingMaster> wings,
        List<FloorMaster> floors,
        List<FlatMaster> flats)
    {
        if (wings.Count != wingIds.Count)
        {
            throw new InvalidOperationException("One or more selected wings are invalid or inactive.");
        }

        if (floors.Count != floorIds.Count)
        {
            throw new InvalidOperationException("One or more selected floors are invalid or inactive.");
        }

        if (flats.Count != flatIds.Count)
        {
            throw new InvalidOperationException("One or more selected flats are invalid.");
        }
    }

    private async Task<PmSocietyWingFlatConfig> UpsertMappingAsync(
        int societyId,
        int wingId,
        int floorId,
        int flatTypeId,
        CancellationToken cancellationToken)
    {
        var existing = await _context.PmSocietyWingFlatConfigs
            .Include(x => x.FlatType)
            .FirstOrDefaultAsync(x =>
                x.SocietyId == societyId &&
                x.WingId == wingId &&
                x.FloorId == floorId &&
                x.FlatTypeId == flatTypeId,
                cancellationToken);

        if (existing is not null)
        {
            if (existing.IsActive)
            {
                throw new InvalidOperationException("This society mapping already exists.");
            }

            existing.IsActive = true;
            return existing;
        }

        var flatType = await _context.FlatCategoryMasters
            .FirstOrDefaultAsync(x => x.Id == flatTypeId, cancellationToken)
            ?? throw new InvalidOperationException("Selected Flat Type is invalid.");

        var config = new PmSocietyWingFlatConfig
        {
            SocietyId = societyId,
            WingId = wingId,
            FloorId = floorId,
            FlatTypeId = flatTypeId,
            FlatType = flatType,
            IsActive = true
        };

        await _context.PmSocietyWingFlatConfigs.AddAsync(config, cancellationToken);
        return config;
    }

    private async Task ValidateWingFlatUniquenessAsync(
        int societyId,
        int wingId,
        int floorId,
        int flatTypeId,
        CancellationToken cancellationToken)
    {
        var duplicate = await _context.PmSocietyWingFlatConfigs
            .AsNoTracking()
            .AnyAsync(x =>
                x.SocietyId == societyId &&
                x.WingId == wingId &&
                x.FloorId == floorId &&
                x.FlatTypeId == flatTypeId &&
                x.IsActive,
                cancellationToken);

        if (duplicate)
        {
            var typeName = await _context.FlatCategoryMasters
                .AsNoTracking()
                .Where(x => x.Id == flatTypeId)
                .Select(x => x.Type)
                .FirstOrDefaultAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Flat Type {typeName ?? flatTypeId.ToString()} is already mapped to this floor.");
        }
    }

    private async Task ValidateMasterReferencesAsync(
        int wingId,
        int floorId,
        int flatTypeId,
        CancellationToken cancellationToken)
    {
        var wingActive = await _context.WingMasters
            .AnyAsync(x => x.Id == wingId && x.IsActive, cancellationToken);

        if (!wingActive)
        {
            throw new InvalidOperationException("Selected wing is invalid or inactive.");
        }

        var floorActive = await _context.FloorMasters
            .AnyAsync(x => x.Id == floorId && x.IsActive, cancellationToken);

        if (!floorActive)
        {
            throw new InvalidOperationException("Selected floor is invalid or inactive.");
        }

        var flatTypeExists = await _context.FlatCategoryMasters.AnyAsync(x => x.Id == flatTypeId, cancellationToken);

        if (!flatTypeExists)
        {
            throw new InvalidOperationException("Selected flat type is invalid.");
        }
    }

    private async Task EnsureSocietyExistsAsync(int societyId, CancellationToken cancellationToken)
    {
        var exists = await _context.SocietyMasters.AnyAsync(x => x.Id == societyId, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException("Society not found.");
        }
    }

    private async Task<List<PmSocietyWingFlatConfig>> GetActiveConfigsForSocietyAsync(int societyId, CancellationToken cancellationToken)
    {
        return await _context.PmSocietyWingFlatConfigs
            .AsNoTracking()
            .Where(x => x.SocietyId == societyId && x.IsActive)
            .ToListAsync(cancellationToken);
    }

    private static SocietyResponseDto MapSocietySummary(SocietyMaster society, IEnumerable<PmSocietyWingFlatConfig> configs)
    {
        var configList = configs.ToList();
        return new SocietyResponseDto
        {
            Id = society.Id,
            Code = society.Code,
            Name = society.Name,
            Location = society.Location,
            WingCount = configList.Select(x => x.WingId).Distinct().Count(),
            FloorCount = configList.Select(x => new { x.WingId, x.FloorId }).Distinct().Count(),
            FlatCount = configList.Count,
            IsConfigured = configList.Count > 0
        };
    }

    private static List<SocietyWingNodeDto> BuildStructureTree(List<PmSocietyWingFlatConfig> configs)
    {
        return configs
            .GroupBy(x => x.WingId)
            .Select(wingGroup =>
            {
                var wing = wingGroup.First().Wing;
                return new SocietyWingNodeDto
                {
                    Wing = new WingResponseDto
                    {
                        Id = wing?.Id ?? wingGroup.Key,
                        Code = wing?.Code ?? string.Empty,
                        Name = wing?.Name ?? string.Empty,
                        IsActive = wing?.IsActive ?? true,
                        FloorCount = wingGroup.Select(x => x.FloorId).Distinct().Count(),
                        FlatCount = wingGroup.Count()
                    },
                    Floors = wingGroup
                        .GroupBy(x => x.FloorId)
                        .Select(floorGroup =>
                        {
                            var floor = floorGroup.First().Floor;
                            return new SocietyFloorNodeDto
                            {
                                Floor = new FloorResponseDto
                                {
                                    Id = floor?.Id ?? floorGroup.Key,
                                    SocietyID = floor?.SocietyID ?? 0,
                                    WingID = floor?.WingID ?? 0,
                                    Code = floor?.Code ?? string.Empty,
                                    Name = floor?.Name ?? string.Empty,
                                    FloorNumber = floor?.FloorNumber ?? 0,
                                    IsActive = floor?.IsActive ?? true,
                                    FlatCount = floorGroup.Count()
                                },
                                Flats = floorGroup
                                    .Select(x => MapConfigToFlatDto(x))
                                    .OrderBy(x => x.Code)
                                    .ToList()
                            };
                        })
                        .OrderBy(x => x.Floor.FloorNumber)
                        .ToList()
                };
            })
            .OrderBy(x => x.Wing.Name)
            .ToList();
    }

    private static FlatResponseDto MapConfigToFlatDto(PmSocietyWingFlatConfig config)
    {
        var code = !string.IsNullOrWhiteSpace(config.FlatNumber)
            ? config.FlatNumber
            : (config.FlatType?.Type ?? string.Empty);

        return new FlatResponseDto
        {
            Id = config.Id,
            Code = code,
            FlatNumber = config.FlatNumber,
            TypeId = config.FlatTypeId,
            TypeName = config.FlatType?.Type,
            IsActive = config.IsActive
        };
    }

    private async Task<string> GenerateSocietyCodeAsync(CancellationToken cancellationToken)
    {
        var next = (await _context.SocietyMasters.MaxAsync(x => (int?)x.Id, cancellationToken) ?? 0) + 1;
        return $"SOC{next:D3}";
    }

    // ────────────── Wing Master CRUD ──────────────

    public async Task<List<WingListDto>> GetAllWingsAsync(
        int societyId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WingMasters
            .AsNoTracking()
            .AsQueryable();

        // Society-wise filter
        query = query.Where(x => x.SocietyID == societyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();

            query = query.Where(x =>
                x.Code.ToLower().Contains(term) ||
                x.Name.ToLower().Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new WingListDto
            {
                Id = x.Id,
                SocietyID = x.SocietyID,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }
    public async Task<WingListDto?> GetWingByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var wing = await _context.WingMasters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (wing is null) return null;
        return new WingListDto { Id = wing.Id, Code = wing.Code, Name = wing.Name, IsActive = wing.IsActive };
    }

    public async Task<WingListDto> CreateWingAsync(CreateWingDto dto, CancellationToken cancellationToken = default)
    {
        var code = dto.Code.Trim();
        var name = dto.Name.Trim();

        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Wing Code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Wing Name is required.");

        if (await _context.WingMasters.AnyAsync(x => x.Code.ToLower() == code.ToLower(), cancellationToken))
            throw new InvalidOperationException("A wing with this code already exists.");

        if (await _context.WingMasters.AnyAsync(x => x.Name.ToLower() == name.ToLower() && x.IsActive, cancellationToken))
            throw new InvalidOperationException("An active wing with this name already exists.");

        var wing = new WingMaster { SocietyID = dto.SocietyID, Code = code, Name = name, IsActive = dto.IsActive };
        await _context.WingMasters.AddAsync(wing, cancellationToken);
        Console.WriteLine($"DTO SocietyID = {dto.SocietyID}");
        Console.WriteLine($"Wing SocietyID = {wing.SocietyID}");
        await _context.SaveChangesAsync(cancellationToken);

        return new WingListDto { Id = wing.Id, Code = wing.Code, Name = wing.Name, IsActive = wing.IsActive };
    }

    public async Task<FloorListDto> CreateFloorAsync(
    CreateFloorDto dto,
    CancellationToken cancellationToken = default)
    {
        if (dto.SocietyId <= 0)
            throw new InvalidOperationException("Society is required.");

        if (dto.WingId <= 0)
            throw new InvalidOperationException("Wing is required.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Floor name is required.");

        var wingExists = await _context.WingMasters
            .AnyAsync(
                x => x.Id == dto.WingId &&
                     x.SocietyID == dto.SocietyId &&
                     x.IsActive,
                cancellationToken);

        if (!wingExists)
            throw new InvalidOperationException(
                "Selected wing does not belong to the selected society.");

        var floor = new FloorMaster
        {
            SocietyID = dto.SocietyId,
            WingID = dto.WingId,
            Name = dto.Name.Trim(),
            FloorNumber = dto.FloorNumber,
            IsActive = dto.IsActive
        };

        await _context.FloorMasters.AddAsync(floor, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new FloorListDto
        {
            Id = floor.Id,
            SocietyID = floor.SocietyID,
            WingID = floor.WingID,
            Name = floor.Name,
            FloorNumber = floor.FloorNumber,
            IsActive = floor.IsActive
        };
    }





    public async Task<WingListDto> UpdateWingAsync(int id, UpdateWingDto dto, CancellationToken cancellationToken = default)
    {
        var wing = await _context.WingMasters.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Wing not found.");

        var code = dto.Code.Trim();
        var name = dto.Name.Trim();

        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Wing Code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Wing Name is required.");

        if (await _context.WingMasters.AnyAsync(x => x.Id != id && x.Code.ToLower() == code.ToLower(), cancellationToken))
            throw new InvalidOperationException("A wing with this code already exists.");

        if (await _context.WingMasters.AnyAsync(x => x.Id != id && x.Name.ToLower() == name.ToLower() && x.IsActive, cancellationToken))
            throw new InvalidOperationException("An active wing with this name already exists.");

        wing.Code = code;
        wing.Name = name;
        wing.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        return new WingListDto { Id = wing.Id, Code = wing.Code, Name = wing.Name, IsActive = wing.IsActive };
    }

    public async Task DeleteWingAsync(int id, CancellationToken cancellationToken = default)
    {
        var wing = await _context.WingMasters.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Wing not found.");

        // Check if wing is referenced by any active society configuration
        var hasConfig = await _context.PmSocietyWingFlatConfigs
            .AnyAsync(x => x.WingId == id && x.IsActive, cancellationToken);

        if (hasConfig)
            throw new InvalidOperationException("Cannot delete this wing because it is used in an active society configuration.");

        // Soft delete: set IsActive = false
        wing.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
    }


    public async Task<PmFlatConfigResponseDto> CreateFlatConfigAsync(
    CreatePmFlatConfigDto dto,
    CancellationToken cancellationToken = default)
    {
        if (dto.SocietyId <= 0)
            throw new InvalidOperationException("Society is required.");

        if (dto.WingId <= 0)
            throw new InvalidOperationException("Wing is required.");

        if (dto.FloorId <= 0)
            throw new InvalidOperationException("Floor is required.");

        if (string.IsNullOrWhiteSpace(dto.Code))
            throw new InvalidOperationException("Flat number is required.");

        if (dto.TypeId <= 0)
            throw new InvalidOperationException("Flat type is required.");

        var code = dto.Code.Trim();

        // 1. Validate Society
        var societyExists = await _context.PmAccounts
            .AnyAsync(
                x => x.ID == dto.SocietyId,
                cancellationToken);

        if (!societyExists)
            throw new InvalidOperationException("Selected society does not exist.");

        // 2. Validate Wing belongs to Society
        var wingExists = await _context.WingMasters
            .AnyAsync(
                x => x.Id == dto.WingId &&
                     x.SocietyID == dto.SocietyId &&
                     x.IsActive,
                cancellationToken);

        if (!wingExists)
            throw new InvalidOperationException(
                "Selected wing does not belong to the selected society.");

        // 3. Validate Floor belongs to Wing + Society
        var floorExists = await _context.FloorMasters
            .AnyAsync(
                x => x.Id == dto.FloorId &&
                     x.WingID == dto.WingId &&
                     x.SocietyID == dto.SocietyId &&
                     x.IsActive,
                cancellationToken);

        if (!floorExists)
            throw new InvalidOperationException(
                "Selected floor does not belong to the selected wing.");

        // 4. Validate Flat Type
        var flatType = await _context.FlatCategoryMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == dto.TypeId,
                cancellationToken);

        if (flatType is null)
            throw new InvalidOperationException(
                "Selected flat type does not exist.");

        // 5. Check duplicate Flat Number
        var duplicateFlat = await _context.PmFlatConfigs
            .AnyAsync(
                x => x.FlatNumber == code &&
                     x.IsActive,
                cancellationToken);

        if (duplicateFlat)
            throw new InvalidOperationException(
                $"Flat number '{code}' already exists.");

        // 6. Create pmFlatConfig
        var flatConfig = new PmFlatConfig
        {
            FlatNumber = code,
            FlatTypeID = dto.TypeId,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        await _context.PmFlatConfigs.AddAsync(
            flatConfig,
            cancellationToken);

        // IMPORTANT:
        // Save first so database generates pmFlatConfig.ID.
        await _context.SaveChangesAsync(cancellationToken);

        // 7. Use generated pmFlatConfig.ID as FlatID
        var mapping = new PmSocietyWingFlatConfig
        {
            SocietyId = dto.SocietyId,
            WingId = dto.WingId,
            FloorId = dto.FloorId,
            FlatID = flatConfig.Id,
            FlatTypeId = dto.TypeId,
            FlatNumber = code,
            IsActive = true
        };

        await _context.PmSocietyWingFlatConfigs.AddAsync(
            mapping,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        // 8. Return response
        return new PmFlatConfigResponseDto
        {
            Id = flatConfig.Id,
            SocietyId = dto.SocietyId,
            WingId = dto.WingId,
            FloorId = dto.FloorId,
            Code = flatConfig.FlatNumber,
            TypeId = flatConfig.FlatTypeID,
            TypeName = flatType.Type,
            IsActive = flatConfig.IsActive
        };
    }



    public async Task<PmFlatConfigResponseDto> CreateFlatAsync(
    CreateFlatDto dto,
    CancellationToken cancellationToken = default)
    {
        if (dto.SocietyId <= 0)
            throw new InvalidOperationException("Society is required.");

        if (dto.WingId <= 0)
            throw new InvalidOperationException("Wing is required.");

        if (dto.FloorId <= 0)
            throw new InvalidOperationException("Floor is required.");

        if (string.IsNullOrWhiteSpace(dto.Code))
            throw new InvalidOperationException("Flat number is required.");

        if (dto.TypeId <= 0)
            throw new InvalidOperationException("Flat type is required.");

        var code = dto.Code.Trim();

        // Validate Society
        var societyExists = await _context.PmAccounts
            .AnyAsync(
                x => x.ID == dto.SocietyId,
                cancellationToken);

        if (!societyExists)
            throw new InvalidOperationException(
                "Selected society does not exist.");

        // Validate Wing belongs to Society
        var wingExists = await _context.WingMasters
            .AnyAsync(
                x => x.Id == dto.WingId &&
                     x.SocietyID == dto.SocietyId &&
                     x.IsActive,
                cancellationToken);

        if (!wingExists)
            throw new InvalidOperationException(
                "Selected wing does not belong to the selected society.");

        // Validate Floor belongs to Wing
        var floorExists = await _context.FloorMasters
            .AnyAsync(
                x => x.Id == dto.FloorId &&
                     x.WingID == dto.WingId &&
                     x.SocietyID == dto.SocietyId &&
                     x.IsActive,
                cancellationToken);

        if (!floorExists)
            throw new InvalidOperationException(
                "Selected floor does not belong to the selected wing.");

        // Validate Flat Type
        var flatType = await _context.FlatCategoryMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == dto.TypeId,
                cancellationToken);

        if (flatType is null)
            throw new InvalidOperationException(
                "Selected flat type does not exist.");

        // Check duplicate flat number
        var duplicate = await _context.PmFlatConfigs
            .AnyAsync(
                x => x.FlatNumber == code &&
                     x.IsActive,
                cancellationToken);

        if (duplicate)
            throw new InvalidOperationException(
                $"Flat number '{code}' already exists.");

        // Create pmFlatConfig
        var flat = new PmFlatConfig
        {
            FlatNumber = code,
            FlatTypeID = dto.TypeId,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        await _context.PmFlatConfigs.AddAsync(
            flat,
            cancellationToken);

        await _context.SaveChangesAsync(
            cancellationToken);

        // Generated pmFlatConfig.ID
        // will be stored as FlatID
        // in pmSocietyWingFlatConfig.

        var mapping = new PmSocietyWingFlatConfig
        {
            SocietyId = dto.SocietyId,
            WingId = dto.WingId,
            FloorId = dto.FloorId,
            FlatID = flat.Id,
            FlatTypeId = dto.TypeId,
            FlatNumber = code,
            IsActive = true
        };

        await _context.PmSocietyWingFlatConfigs.AddAsync(
            mapping,
            cancellationToken);

        await _context.SaveChangesAsync(
            cancellationToken);

        return new PmFlatConfigResponseDto
        {
            Id = flat.Id,
            SocietyId = dto.SocietyId,
            WingId = dto.WingId,
            FloorId = dto.FloorId,
            Code = flat.FlatNumber,
            TypeId = flat.FlatTypeID,
            TypeName = flatType.Type,
            IsActive = flat.IsActive
        };
    }
}
