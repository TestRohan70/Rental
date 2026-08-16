using System.ComponentModel.DataAnnotations.Schema;

namespace RentalAPI.Models;

[Table("society")]
public class SocietyMaster
{
    [Column("ID")]
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Location { get; set; }
}
