namespace RentalAPI.DTO
{
    public class PmAccountDto
    {
        public int ID { get; set; }

        public string? SocName { get; set; }

        public string? Code { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public bool IsActive { get; set; }

        public SpcDetailDto? SpcDtl { get; set; }
    }

    public class SpcDetailDto
    {
        public string? Name { get; set; }

        public string? Designation { get; set; }

        public string? Contact { get; set; }
    }
}
