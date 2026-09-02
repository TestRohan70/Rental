using System.ComponentModel.DataAnnotations.Schema;

namespace RentalAPI.Models;

[Table("VisitorStatus")]
public partial class VisitorStatus
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
