using System.ComponentModel.DataAnnotations;

namespace QRCertificateSystem.Models
{
    public enum UserRole
    {
        SuperAdmin = 0,
        UniversityAdmin = 1,
        Student = 2
    }

    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Student;

        public int? UniversityId { get; set; }

        [StringLength(200)]
        public string? ProfileImageUrl { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;
        public bool EmailConfirmed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Navigation Properties
        public virtual University? University { get; set; }
        public virtual Student? Student { get; set; }
        public virtual ICollection<Certificate> CreatedCertificates { get; set; } = new List<Certificate>();

        // Full Name Property
        public string FullName => $"{FirstName} {LastName}";
    }
}