using System.ComponentModel.DataAnnotations;

public class CreateFloorDto
{
    [Required]
    public int SocietyId { get; set; }

    [Required]
    public int WingId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int FloorNumber { get; set; }

    public bool IsActive { get; set; } = true;
}