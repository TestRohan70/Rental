using System.ComponentModel.DataAnnotations.Schema;

namespace RentalAPI.Models;

[Table("Flat")]
public class FlatMaster
{
    [Column("ID")]
    public int Id { get; set; }

    [Column("CODE")]
    public string Code { get; set; } = null!;

    [Column("TypeID")]
    public int? TypeId { get; set; }

    public virtual FlatCategoryMaster? Type { get; set; }
}
