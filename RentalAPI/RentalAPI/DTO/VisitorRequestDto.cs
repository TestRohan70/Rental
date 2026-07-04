namespace RentalAPI.DTO;

public class VisitorRequestDto
{
    public int Id { get; set; }

    public string VisitorName { get; set; } = string.Empty;

    public string? VisitorPhone { get; set; }

    public string? Purpose { get; set; }

    public string Wing { get; set; } = string.Empty;

    public int FlatNo { get; set; }

    public int ResidentId { get; set; }

    public string ResidentName { get; set; } = string.Empty;

    public int SecurityId { get; set; }

    public string SecurityName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public DateTime? RespondedDate { get; set; }

    public DateTime? AcknowledgedDate { get; set; }

    public string? VisitorPhotoUrl { get; set; }
}
