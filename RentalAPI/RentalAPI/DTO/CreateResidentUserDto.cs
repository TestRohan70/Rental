namespace RentalAPI.DTO;

public class CreateResidentUserDto
{
    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Address { get; set; }

    public bool? Parking { get; set; }

    public int? NoOfParking { get; set; }

    public int SocietyWingFlatConfigId { get; set; }

    public string OwnershipType { get; set; } = "Owner";
}
