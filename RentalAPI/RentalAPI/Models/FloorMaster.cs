using System.ComponentModel.DataAnnotations.Schema;

namespace RentalAPI.Models;

[Table("floors")]
public class FloorMaster
{
    [Column("ID")]
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int FloorNumber { get; set; }

    public bool IsActive { get; set; } = true;
}
