namespace RentalAPI.Models
{
    public class SocietySecurityMapping
    {
        public int ID { get; set; }

        public int SocietyID { get; set; }

        public int UserID { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsActive { get; set; }

        public SysmUser? User { get; set; }
    }
}
