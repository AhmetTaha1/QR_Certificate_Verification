namespace QRCertificateSystem.Models
{
    public class VerificationLog
    {
        public int Id { get; set; }
        public int CertificateId { get; set; }
        public string IPAddress { get; set; } = string.Empty;
        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
        public bool IsValid { get; set; }
        
        // Navigation property
        public Certificate? Certificate { get; set; }
    }
}