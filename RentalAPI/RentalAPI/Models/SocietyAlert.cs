namespace RentalAPI.Models;

public partial class SocietyAlert
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string AlertType { get; set; } = "General";

    public int CreatedBySecurityId { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Resident? CreatedBySecurity { get; set; }
}
