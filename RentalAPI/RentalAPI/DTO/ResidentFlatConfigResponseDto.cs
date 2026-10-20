namespace RentalAPI.DTO;

public class ResidentFlatConfigResponseDto
{
    public int SocietyWingFlatConfigId { get; set; }
    public int FlatId { get; set; }
    public string? FlatNumber { get; set; }
    public string? FlatTypeName { get; set; }
    public int FloorId { get; set; }
    public string FloorName { get; set; } = string.Empty;
    public int WingId { get; set; }
    public string WingName { get; set; } = string.Empty;
}
