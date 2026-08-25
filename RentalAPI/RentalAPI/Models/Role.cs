using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RentalAPI.Models;

[Table("Role")]
public class Role
{
    [Column("ID")]
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public virtual ICollection<SysmUser> Users { get; set; } = new List<SysmUser>();
}
