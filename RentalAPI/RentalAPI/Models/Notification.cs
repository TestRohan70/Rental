using System;
using System.Collections.Generic;

namespace RentalAPI.Models;

public partial class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? ResidentId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Resident? Resident { get; set; }

    public virtual SysmUser User { get; set; } = null!;
}
