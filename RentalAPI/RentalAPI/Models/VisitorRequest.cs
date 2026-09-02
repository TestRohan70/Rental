using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RentalAPI.Models;

[Table("VisitorRequest")]
public partial class VisitorRequest
{
    public int Id { get; set; }

    public string VisitorName { get; set; } = null!;

    public string? VisitorPhone { get; set; }

    public string? Purpose { get; set; }

    // Legacy nullable fields
    public string? Wing { get; set; }

    public int? FlatNo { get; set; }

    public int ResidentId { get; set; }

    public int? SecurityUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? RespondedDate { get; set; }

    public DateTime? AcknowledgedDate { get; set; }

    public string? VisitorPhotoUrl { get; set; }

    public int? SocietyId { get; set; }

    public int? SocietyWingFlatConfigId { get; set; }

    public string? VisitType { get; set; }

    public DateTime? ExpectedArrivalDateTime { get; set; }

    public string? OTPHash { get; set; }

    public DateTime? OTPExpiresAt { get; set; }

    public DateTime? OTPVerifiedAt { get; set; }

    public int? StatusId { get; set; }

    [JsonIgnore]
    [ForeignKey("ResidentId")]
    public virtual Resident Resident { get; set; } = null!;

    [JsonIgnore]
    [ForeignKey("SecurityUserId")]
    public virtual SysmUser? SecurityUser { get; set; }

    [JsonIgnore]
    [ForeignKey("SocietyId")]
    public virtual SocietyMaster? Society { get; set; }

    [JsonIgnore]
    [ForeignKey("SocietyWingFlatConfigId")]
    public virtual PmSocietyWingFlatConfig? SocietyWingFlatConfig { get; set; }

    [JsonIgnore]
    [ForeignKey("StatusId")]
    public virtual VisitorStatus? Status { get; set; }

    public virtual ICollection<VisitorVisit> VisitorVisits { get; set; } = new List<VisitorVisit>();
}
