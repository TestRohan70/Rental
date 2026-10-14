using System.ComponentModel.DataAnnotations;

namespace RentalAPI.DTO
{
    public class CreateFlatDto
    {
        [Required]
        public int SocietyId { get; set; }

        [Required]
        public int WingId { get; set; }

        [Required]
        public int FloorId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public int TypeId { get; set; }

        public string? TypeName { get; set; }
    }
}
