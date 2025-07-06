using System.ComponentModel.DataAnnotations;

namespace CarbonZero.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string Username { get; set; }

        [Required]
        public required string IdNo { get; set; }

        [Required]
        public required string Password { get; set; }

        [Required]
        public double PowerCapacityKW { get; set; }
    }
}
