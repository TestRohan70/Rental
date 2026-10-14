using System.ComponentModel.DataAnnotations;

namespace RentalAPI.DTO.PAdmin;

public class SocietyResponseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int WingCount { get; set; }
    public int FloorCount { get; set; }
    public int FlatCount { get; set; }
    public bool IsConfigured { get; set; }
}

public class CreateSocietyDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Location { get; set; }
}

public class UpdateSocietyDto : CreateSocietyDto
{
}

public class WingResponseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int FloorCount { get; set; }
    public int FlatCount { get; set; }
}

public class FloorResponseDto
{
    public int Id { get; set; }
    public int SocietyID { get; set; }
    public int WingID { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public bool IsActive { get; set; }
    public int FlatCount { get; set; }
}

public class FlatResponseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? FlatNumber { get; set; }
    public int? TypeId { get; set; }
    public string? TypeName { get; set; }
    public int FlatTypeId => TypeId ?? Id;
    public string FlatTypeName => TypeName ?? Code;
    public bool IsActive { get; set; }
}

public class SocietyFlatConfigResponseDto
{
    public int Id { get; set; }
    public int FlatId { get; set; }
    public int SocietyId { get; set; }
    public int WingId { get; set; }
    public string WingCode { get; set; } = string.Empty;
    public string WingName { get; set; } = string.Empty;
    public int FloorId { get; set; }
    public string FloorName { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public string Code { get; set; } = string.Empty;
    public int TypeId { get; set; }
    public string? TypeName { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSocietyMappingDto
{
    [Required]
    public int WingId { get; set; }

    [Required]
    public int FloorId { get; set; }

    public int? FlatTypeId { get; set; }
    public int? FlatId { get; set; }
    public string? FlatNumber { get; set; }

    public int ResolvedFlatTypeId => (FlatTypeId is > 0 ? FlatTypeId.Value : (FlatId ?? 0));
}

public class SocietyStructureDto
{
    public SocietyResponseDto Society { get; set; } = null!;
    public List<SocietyWingNodeDto> Wings { get; set; } = [];
}

public class SocietyWingNodeDto
{
    public WingResponseDto Wing { get; set; } = null!;
    public List<SocietyFloorNodeDto> Floors { get; set; } = [];
}

public class SocietyFloorNodeDto
{
    public FloorResponseDto Floor { get; set; } = null!;
    public List<FlatResponseDto> Flats { get; set; } = [];
}

public class GenerateSocietyStructureDto
{
    [MinLength(1)]
    public List<int> WingIds { get; set; } = [];

    [MinLength(1)]
    public List<int> FloorIds { get; set; } = [];

    public List<int> FlatTypeIds { get; set; } = [];
    public List<int> FlatIds { get; set; } = [];

    public List<int> ResolvedFlatTypeIds => (FlatTypeIds != null && FlatTypeIds.Count > 0) ? FlatTypeIds : (FlatIds ?? []);

    public bool PreviewOnly { get; set; }
}

public class GenerateStructurePreviewDto
{
    public int TotalWings { get; set; }
    public int TotalFloors { get; set; }
    public int TotalFlats { get; set; }
    public int TotalMappings { get; set; }
    public int SkippedDuplicates { get; set; }
    public List<SocietyWingNodeDto> Preview { get; set; } = [];
}

public class WingListDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public  int  SocietyID { get; set; }
}

public class CreateWingDto
{
    [Required]
    public int SocietyID { get; set; }

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdateWingDto
{
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
