using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RentalAPI.Models;

[Table("VisitorVisit")]
public partial class VisitorVisit
{
    public int Id { get; set; }

    public int VisitorRequestId { get; set; }

    public DateTime? CheckInDateTime { get; set; }

    public DateTime? CheckOutDateTime { get; set; }

    public int? CheckInSecurityUserId { get; set; }

    public int? CheckOutSecurityUserId { get; set; }

    public string? Gate { get; set; }

    public int StatusId { get; set; }

    public DateTime CreatedDate { get; set; }

    [JsonIgnore]
    public virtual VisitorRequest VisitorRequest { get; set; } = null!;

    [JsonIgnore]
    [ForeignKey("CheckInSecurityUserId")]
    public virtual SysmUser? CheckInSecurityUser { get; set; }

    [JsonIgnore]
    [ForeignKey("CheckOutSecurityUserId")]
    public virtual SysmUser? CheckOutSecurityUser { get; set; }

    [JsonIgnore]
    [ForeignKey("StatusId")]
    public virtual VisitorStatus Status { get; set; } = null!;
}
