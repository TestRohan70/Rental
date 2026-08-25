using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RentalAPI.Models;

[Table("SocietyUserMapping")]
public class SocietyUserMapping
{
    [Column("ID")]
    public int Id { get; set; }

    [Column("SocietyID")]
    public int SocietyId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public virtual SocietyMaster Society { get; set; } = null!;

    [JsonIgnore]
    public virtual SysmUser User { get; set; } = null!;
}
