using Microsoft.EntityFrameworkCore;
using QRCertificateSystem.Data;
using QRCertificateSystem.Models;

namespace QRCertificateSystem.Services
{
    public enum VerificationStatus
    {
        NotFound,
        Valid,
        Revoked,
        NotIssued,
        Tampered
    }

    public sealed record VerificationResult(string CertificateId, VerificationStatus Status, Certificate? Certificate)
    {
        public bool IsValid => Status == VerificationStatus.Valid;
    }

    public class CertificateVerificationService(ApplicationDbContext context, CertificateSigner signer, ILogger<CertificateVerificationService> logger)
    {
        private readonly ApplicationDbContext _context = context;
        private readonly CertificateSigner _signer = signer;
        private readonly ILogger<CertificateVerificationService> _logger = logger;

        public async Task<VerificationResult> VerifyAsync(string certificateId, string? ipAddress, CancellationToken ct = default)
        {
            var id = certificateId.Trim();

            var certificate = await _context.Certificates
                .AsNoTracking()
                .Include(c => c.University)
                .FirstOrDefaultAsync(c => c.CertificateId == id, ct);

            if (certificate == null)
            {
                return new VerificationResult(id, VerificationStatus.NotFound, null);
            }

            var status = Evaluate(certificate, _signer);

            _context.VerificationLogs.Add(new VerificationLog
            {
                CertificateId = certificate.Id,
                IPAddress = ipAddress ?? "unknown",
                VerifiedAt = DateTime.UtcNow,
                IsValid = status == VerificationStatus.Valid
            });

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // Logging the lookup must never block the verification result itself.
                _logger.LogWarning(ex, "Could not store verification log for {CertificateId}", id);
            }

            if (status == VerificationStatus.Tampered)
            {
                _logger.LogWarning("Signature mismatch for certificate {CertificateId}", id);
            }

            return new VerificationResult(id, status, certificate);
        }

        internal static VerificationStatus Evaluate(Certificate certificate, CertificateSigner signer)
        {
            // The signature is checked first: a forged row must never be reported as "revoked" or similar.
            if (!signer.IsValid(certificate))
            {
                return VerificationStatus.Tampered;
            }

            return certificate.Status switch
            {
                CertificateStatus.Approved => VerificationStatus.Valid,
                CertificateStatus.Revoked => VerificationStatus.Revoked,
                _ => VerificationStatus.NotIssued
            };
        }
    }
}
