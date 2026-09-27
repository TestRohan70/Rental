namespace RentalAPI.DTO
{
    public class FloorListDto
    {
        public int Id { get; set; }

        public int SocietyID { get; set; }

        public int WingID { get; set; }

        public string? Code { get; set; }

        public string Name { get; set; } = string.Empty;

        public int FloorNumber { get; set; }

        public bool IsActive { get; set; }
    }
}