using System.ComponentModel.DataAnnotations.Schema;

namespace RentalAPI.Models;

[Table("pmSocietyWingFlatConfig")]
public class PmSocietyWingFlatConfig
{
    [Column("ID")]
    public int Id { get; set; }

    [Column("SocietyID")]
    public int SocietyId { get; set; }

    [Column("WingID")]
    public int WingId { get; set; }

    [Column("FloorID")]
    public int FloorId { get; set; }

    [Column("FlatID")]
    public int FlatId { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual SocietyMaster Society { get; set; } = null!;

    public virtual WingMaster Wing { get; set; } = null!;

    public virtual FloorMaster Floor { get; set; } = null!;

    public virtual FlatMaster Flat { get; set; } = null!;
}
