using System.ComponentModel.DataAnnotations.Schema;

namespace RentalAPI.Models;

[Table("FlatCategory")]
public class FlatCategoryMaster
{
    [Column("ID")]
    public int Id { get; set; }

    [Column("Type")]
    public string? Type { get; set; }
}
