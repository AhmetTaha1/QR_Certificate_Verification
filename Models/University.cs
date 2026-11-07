using System.ComponentModel.DataAnnotations;

namespace QRCertificateSystem.Models
{
    public class University
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string ShortName { get; set; } = string.Empty; // DÜ, SAÜ, KOÜ

        [StringLength(500)]
        public string? LogoUrl { get; set; }

        [StringLength(7)]
        public string? PrimaryColor { get; set; } = "#667eea"; // Hex color

        [StringLength(7)]
        public string? SecondaryColor { get; set; } = "#764ba2";

        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Country { get; set; } = "Turkey";

        public bool IsActive { get; set; } = false; // SuperAdmin approval

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    }
}
