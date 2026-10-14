using System.ComponentModel.DataAnnotations;

namespace RentalAPI.DTO
{
    public class UpdatePmFlatConfigDto
    {
        [Required]
        [MaxLength(50)]
        public string FlatNumber { get; set; } = string.Empty;

        [Required]
        public int FlatTypeID { get; set; }

        public bool IsActive { get; set; }
    }
}
