using System.ComponentModel.DataAnnotations.Schema;

namespace RentalAPI.Models;

[Table("pmFlatConfig")]
public class PmFlatConfig
{
    [Column("ID")]
    public int Id { get; set; }

    public string FlatNumber { get; set; } = string.Empty;

    public int FlatTypeID { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public FlatCategoryMaster? FlatType { get; set; }
}