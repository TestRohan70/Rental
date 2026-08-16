using System.ComponentModel.DataAnnotations.Schema;

namespace RentalAPI.Models;

[Table("wings")]
public class WingMaster
{
    [Column("ID")]
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}
