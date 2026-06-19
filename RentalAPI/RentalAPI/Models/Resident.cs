using System;
using System.Collections.Generic;

namespace RentalAPI.Models;

public partial class Resident
{
    public int Id { get; set; }

    public string? Society { get; set; }

    public string Wing { get; set; } = null!;

    public int FlatNo { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Address { get; set; }

    public bool? Parking { get; set; }

    public int? NoofParking { get; set; }

    public string? OwnershipType { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? Status { get; set; }

    public int? ApprovedBy { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
