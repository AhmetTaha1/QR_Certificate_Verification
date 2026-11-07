using System.ComponentModel.DataAnnotations;

namespace QRCertificateSystem.Dtos;

/// <summary>Posted by the create and edit certificate forms.</summary>
public class CertificateFormModel
{
    public static readonly string[] AllowedDegrees = ["Associate", "Bachelor", "Master", "PhD"];

    /// <summary>Optional link to a registered student of the admin's university.</summary>
    public int? StudentId { get; set; }

    /// <summary>Required when no student is linked; defaults to the student's full name otherwise.</summary>
    [StringLength(200)]
    public string? StudentName { get; set; }

    [Required]
    [StringLength(200)]
    public string Department { get; set; } = string.Empty;

    [Required]
    public string Degree { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;

    /// <summary>
    /// Bound as text and parsed with the invariant culture: binding a decimal directly would use the
    /// server culture, where "3.50" can become 350 (e.g. tr-TR).
    /// </summary>
    public string? Gpa { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
