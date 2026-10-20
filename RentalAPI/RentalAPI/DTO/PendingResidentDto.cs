namespace RentalAPI.DTO
{
    public class PendingResidentDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? Name { get; set; }
        public string? Role { get; set; }
        public string? Wing { get; set; }
        public string? flatNo { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? Status { get; set; }
    }
}
