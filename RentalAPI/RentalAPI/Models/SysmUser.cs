using System;
using System.Collections.Generic;

namespace RentalAPI.Models;

public partial class SysmUser
{
    public int Id { get; set; }

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Role { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
