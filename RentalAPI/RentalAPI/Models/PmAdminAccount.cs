namespace RentalAPI.Models
{
    public partial class PmAdminAccount
    {
        public int ID { get; set; }

        public int SocietyID { get; set; }

        public string Name { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string? Phone { get; set; }

        public string Username { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public int? ModifiedBy { get; set; }
    }
}
