using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RentalAPI.Models;

[Table("Resident")]
public partial class Resident
{
    [Column("Id")]
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public bool? Parking { get; set; }

    public int? NoofParking { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? Status { get; set; }

    public int? ApprovedBy { get; set; }

    [JsonIgnore]
    public virtual SysmUser? User { get; set; }

    [JsonIgnore]
    public virtual SysmUser? ApprovedByUser { get; set; }

    [JsonIgnore]
    public virtual ICollection<ResidentFlatMapping> FlatMappings { get; set; } = new List<ResidentFlatMapping>();

    [JsonIgnore]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
