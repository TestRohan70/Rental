using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RentalAPI.Models;

public partial class SysmUser
{
    public int Id { get; set; }

    public string? UserName { get; set; }

    public string? Email { get; set; }

    [JsonIgnore]
    public string? Password { get; set; }

    public string? Role { get; set; }

    [JsonIgnore]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
