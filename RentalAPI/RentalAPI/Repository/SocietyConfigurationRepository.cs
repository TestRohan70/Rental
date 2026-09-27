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
            .Include(x => x.Flat).ThenInclude(f => f.Type)
            .Where(x => x.SocietyId == societyId && x.IsActive)
            .ToListAsync(cancellationToken);

        return new SocietyStructureDto
        {
            Society = society,
            Wings = BuildStructureTree(configs)
        };
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

    public async Task<List<FloorResponseDto>> GetActiveFloorsAsync(int? wingId, CancellationToken cancellationToken = default)
    {
        if (wingId is null or <= 0)
        {
            return await _context.FloorMasters
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.FloorNumber)
                .Select(x => new FloorResponseDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    FloorNumber = x.FloorNumber,
                    IsActive = x.IsActive
                })
                .ToListAsync(cancellationToken);
        }

        var query = _context.FloorMasters
      .AsNoTracking()
      .Where(x => x.IsActive && x.WingID == wingId.Value);

        return await query
            .OrderBy(x => x.FloorNumber)
            .Select(x => new FloorResponseDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                FloorNumber = x.FloorNumber,
                IsActive = x.IsActive
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
        await EnsureSocietyExistsAsync(societyId, cancellationToken);
        await ValidateMasterReferencesAsync(dto.WingId, dto.FloorId, dto.FlatId, cancellationToken);
        await ValidateWingFlatUniquenessAsync(societyId, dto.WingId, dto.FloorId, dto.FlatId, cancellationToken);

        var config = await UpsertMappingAsync(societyId, dto.WingId, dto.FloorId, dto.FlatId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapFlat(config.Flat, config.IsActive);
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
                x.FlatId == flatId &&
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
            .Select(x => new { x.WingId, x.FloorId, x.FlatId })
            .ToListAsync(cancellationToken);

        var existingExact = existingActive
            .Select(x => ToExactMappingKey(x.WingId, x.FloorId, x.FlatId))
            .ToHashSet(StringComparer.Ordinal);

        var existingWingFlat = existingActive
            .Select(x => ToWingFlatKey(x.WingId, x.FlatId))
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
            FlatId = m.FlatId,
            IsActive = true,
            Wing = m.Wing,
            Floor = m.Floor,
            Flat = m.Flat
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
        int flatId,
        CancellationToken cancellationToken)
    {
        var existing = await _context.PmSocietyWingFlatConfigs
            .Include(x => x.Flat).ThenInclude(f => f.Type)
            .FirstOrDefaultAsync(x =>
                x.SocietyId == societyId &&
                x.WingId == wingId &&
                x.FloorId == floorId &&
                x.FlatId == flatId,
                cancellationToken);

        if (existing is not null)
        {
            if (existing.IsActive)
            {
                throw new InvalidOperationException("This society mapping already exists.");
            }

            await ValidateWingFlatUniquenessAsync(societyId, wingId, floorId, flatId, cancellationToken);
            existing.IsActive = true;
            return existing;
        }

        var config = new PmSocietyWingFlatConfig
        {
            SocietyId = societyId,
            WingId = wingId,
            FloorId = floorId,
            FlatId = flatId,
            IsActive = true
        };

        await _context.PmSocietyWingFlatConfigs.AddAsync(config, cancellationToken);

        var flat = await _context.FlatMasters.Include(x => x.Type)
            .FirstAsync(x => x.Id == flatId, cancellationToken);

        config.Flat = flat;
        return config;
    }

    private async Task ValidateWingFlatUniquenessAsync(
        int societyId,
        int wingId,
        int floorId,
        int flatId,
        CancellationToken cancellationToken)
    {
        var duplicateInWing = await _context.PmSocietyWingFlatConfigs
            .AsNoTracking()
            .AnyAsync(x =>
                x.SocietyId == societyId &&
                x.WingId == wingId &&
                x.FlatId == flatId &&
                x.FloorId != floorId &&
                x.IsActive,
                cancellationToken);

        if (duplicateInWing)
        {
            var flatCode = await _context.FlatMasters
                .AsNoTracking()
                .Where(x => x.Id == flatId)
                .Select(x => x.Code)
                .FirstOrDefaultAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Flat {flatCode ?? flatId.ToString()} is already mapped to another floor in this wing.");
        }
    }

    private async Task ValidateMasterReferencesAsync(
        int wingId,
        int floorId,
        int flatId,
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

        var flatExists = await _context.FlatMasters.AnyAsync(x => x.Id == flatId, cancellationToken);

        if (!flatExists)
        {
            throw new InvalidOperationException("Selected flat is invalid.");
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
                        Id = wing.Id,
                        Code = wing.Code,
                        Name = wing.Name,
                        IsActive = wing.IsActive,
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
                                    Id = floor.Id,
                                    Code = floor.Code,
                                    Name = floor.Name,
                                    FloorNumber = floor.FloorNumber,
                                    IsActive = floor.IsActive,
                                    FlatCount = floorGroup.Count()
                                },
                                Flats = floorGroup
                                    .Select(x => MapFlat(x.Flat, x.IsActive))
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

    private static FlatResponseDto MapFlat(FlatMaster flat, bool isActive)
    {
        return new FlatResponseDto
        {
            Id = flat.Id,
            Code = flat.Code,
            TypeId = flat.TypeId,
            TypeName = flat.Type?.Type,
            IsActive = isActive
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
}
