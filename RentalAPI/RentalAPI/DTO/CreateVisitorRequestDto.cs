using System.ComponentModel.DataAnnotations;

namespace RentalAPI.DTO;

public class CreateVisitorRequestDto
{
    [Required]
    public string VisitorName { get; set; } = string.Empty;

    public string? VisitorPhone { get; set; }

    public string? Purpose { get; set; }

    [Required]
    public string Wing { get; set; } = string.Empty;

    [Range(1, 9999)]
    public int FlatNo { get; set; }

    public int SecurityId { get; set; }

    public string? VisitorPhotoUrl { get; set; }
}
