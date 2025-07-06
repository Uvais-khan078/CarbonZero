using System;
using System.ComponentModel.DataAnnotations;

namespace CarbonZero.Models
{
    public class SessionHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public DateTime LoginTime { get; set; }

        public DateTime? LogoutTime { get; set; }

        public double EnergyGenerated { get; set; }

        public double CO2Saved { get; set; }
    }
}
