namespace RentalAPI.DTO
{
    public class PmAccountRequest
    {
        public string? SocName { get; set; }

        public string? Code { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public bool IsActive { get; set; }

        public SpcDetailDto? SpcDtl { get; set; }
    }
}
