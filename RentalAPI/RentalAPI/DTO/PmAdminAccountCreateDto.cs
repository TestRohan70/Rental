namespace RentalAPI.DTO
{
    public class PmAdminAccountCreateDto
    {
        public int SocietyID { get; set; }

        public string Name { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string? Phone { get; set; }

        public string Username { get; set; } = null!;

        public string Password { get; set; } = null!;
    }
}
