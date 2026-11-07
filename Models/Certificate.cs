using System.ComponentModel.DataAnnotations;

namespace QRCertificateSystem.Models
{
    public enum CertificateStatus
    {
        Draft = 0,
        Approved = 1,
        Rejected = 2,
        Revoked = 3
    }

    public class Certificate
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CertificateId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Institution { get; set; } = string.Empty;

        public DateTime IssueDate { get; set; }

        [Required]
        public string HashValue { get; set; } = string.Empty;

        [Required]
        public string QRCodeData { get; set; } = string.Empty;

        public CertificateStatus Status { get; set; } = CertificateStatus.Draft;

        // Foreign Keys
        public int? CreatedByUserId { get; set; }
        public int? StudentId { get; set; }
        public int UniversityId { get; set; }

        [StringLength(50)]
        public string? Degree { get; set; } // Bachelor, Master, PhD

        public decimal? GPA { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual User? CreatedByUser { get; set; }
        public virtual Student? Student { get; set; }
        public virtual University University { get; set; } = null!;
        public virtual ICollection<VerificationLog> VerificationLogs { get; set; } = new List<VerificationLog>();
    }
}