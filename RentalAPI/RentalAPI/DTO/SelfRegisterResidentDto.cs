using System.ComponentModel.DataAnnotations;

namespace RentalAPI.DTO;

public class SelfRegisterResidentDto
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = null!;

    [Required]
    public int SocietyWingFlatConfigId { get; set; }

    [Required]
    public string OwnershipType { get; set; } = "Owner";
}
