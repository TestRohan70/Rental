using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RentalAPI.Models;

[Table("ResidentFlatMapping")]
public class ResidentFlatMapping
{
    [Column("ID")]
    public int Id { get; set; }

    [Column("ResidentID")]
    public int ResidentId { get; set; }

    [Column("SocietyWingFlatConfigID")]
    public int SocietyWingFlatConfigId { get; set; }

    public string? OwnershipType { get; set; }

    public bool IsPrimary { get; set; } = true;

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public virtual Resident Resident { get; set; } = null!;

    [JsonIgnore]
    public virtual PmSocietyWingFlatConfig SocietyWingFlatConfig { get; set; } = null!;
}
