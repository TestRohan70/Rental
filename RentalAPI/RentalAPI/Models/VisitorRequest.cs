using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RentalAPI.Models;

[Table("VisitorRequest")]
public class VisitorRequest
{
    public int Id { get; set; }

    public string VisitorName { get; set; } = null!;

    public string? VisitorPhone { get; set; }

    public string? Purpose { get; set; }

    public string Wing { get; set; } = null!;

    public int FlatNo { get; set; }

    public int ResidentId { get; set; }

    [Column("SecurityId")]
    public int SecurityUserId { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedDate { get; set; }

    public DateTime? RespondedDate { get; set; }

    public DateTime? AcknowledgedDate { get; set; }

    public string? VisitorPhotoUrl { get; set; }

    [JsonIgnore]
    public virtual Resident Resident { get; set; } = null!;

    [JsonIgnore]
    [ForeignKey("SecurityUserId")]
    public virtual SysmUser SecurityUser { get; set; } = null!;
}
