using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RentalAPI.DTO
{
    public class CreateResidentDto
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Wing { get; set; } = string.Empty;

        public int FlatNo { get; set; }

        [Required]
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
