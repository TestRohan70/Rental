namespace RentalAPI.DTO.PAdmin;

public class PmFlatConfigResponseDto
{
    public int Id { get; set; }

    public int SocietyId { get; set; }

    public int WingId { get; set; }

    public int FloorId { get; set; }

    public string Code { get; set; } = string.Empty;

    public int TypeId { get; set; }

    public string? TypeName { get; set; }

    public bool IsActive { get; set; }
}