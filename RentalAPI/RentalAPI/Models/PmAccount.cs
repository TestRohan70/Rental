using System.Text.Json;

namespace RentalAPI.Models
{
    public class PmAccount
    {
        public int ID { get; set; }

        public string? SocName { get; set; }

        public string? Code { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public bool IsActive { get; set; }

        // JSON stored as string so it works with both PostgreSQL and SQL Server
        public JsonDocument? SpcDtl { get; set; }
        public DateTime? CreatedDate { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public int? ModifiedBy { get; set; }
    }
}
