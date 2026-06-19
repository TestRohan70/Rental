namespace RentalAPI.DTO
{
    public class CreateResidentDto
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string Wing { get; set; }

        public int FlatNo { get; set; }

        public string Password { get; set; }
    }
}
