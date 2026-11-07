using System.ComponentModel.DataAnnotations;

namespace QRCertificateSystem.Models
{
    public class Student
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int UniversityId { get; set; }

        [Required]
        [StringLength(50)]
        public string StudentNumber { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Department { get; set; }

        [StringLength(100)]
        public string? Faculty { get; set; }

        public int? EnrollmentYear { get; set; }
        public int? ExpectedGraduationYear { get; set; }
        public int? ActualGraduationYear { get; set; }

        public decimal? GPA { get; set; }

        [StringLength(50)]
        public string? Degree { get; set; } // Bachelor, Master, PhD

        [StringLength(100)]
        public string? Major { get; set; }

        [StringLength(100)]
        public string? Minor { get; set; }

        public bool IsGraduated { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual User User { get; set; } = null!;
        public virtual University University { get; set; } = null!;
        public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    }
}
