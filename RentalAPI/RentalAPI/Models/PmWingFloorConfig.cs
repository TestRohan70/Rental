using System.ComponentModel.DataAnnotations.Schema;

namespace RentalAPI.Models;

[Table("pmWingFloorConfig")]
public class PmWingFloorConfig
{
    [Column("ID")]
    public int Id { get; set; }

    [Column("WingID")]
    public int WingId { get; set; }

    [Column("FloorID")]
    public int FloorId { get; set; }

    public virtual WingMaster Wing { get; set; } = null!;

    public virtual FloorMaster Floor { get; set; } = null!;
}
