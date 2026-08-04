namespace RentalAPI.DTO;

public class CreateSocietyAlertDto
{
    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string AlertType { get; set; } = "General";
}
