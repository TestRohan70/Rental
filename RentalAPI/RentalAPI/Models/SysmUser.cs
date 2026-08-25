using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RentalAPI.Models;

[Table("SysmUser")]
public partial class SysmUser
{
    [Column("ID")]
    public int Id { get; set; }

    public string? UserName { get; set; }

    public string? Email { get; set; }

    [JsonIgnore]
    public string? Password { get; set; }

    // Legacy string column in DB, not used for authorization
    public string? Role { get; set; }

    public int? RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    [JsonIgnore]
    public virtual Role? RoleNavigation { get; set; }

    [JsonIgnore]
    public virtual Resident? Resident { get; set; }

    [JsonIgnore]
    public virtual ICollection<SocietyUserMapping> SocietyUserMappings { get; set; } = new List<SocietyUserMapping>();

    [JsonIgnore]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
