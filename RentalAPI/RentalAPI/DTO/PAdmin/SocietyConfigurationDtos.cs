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
    public int? TypeId { get; set; }
    public string? TypeName { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSocietyMappingDto
{
    [Required]
    public int WingId { get; set; }

    [Required]
    public int FloorId { get; set; }

    [Required]
    public int FlatId { get; set; }
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

    [MinLength(1)]
    public List<int> FlatIds { get; set; } = [];

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
