using QRCertificateSystem.Models;
using QRCertificateSystem.Services;
using Xunit;

namespace QRCertificationSystem.Tests;

public class VerificationStatusTests
{
    private static readonly CertificateSigner Signer = new("unit-test-signing-key-that-is-long-enough-1234");

    private static Certificate Signed(CertificateStatus status)
    {
        var certificate = new Certificate
        {
            CertificateId = "CERT-2026-000000000001",
            Name = "Grace Hopper",
            Department = "Mathematics",
            Institution = "Kocaeli Üniversitesi",
            IssueDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            UniversityId = 3,
            Status = status
        };
        certificate.HashValue = Signer.Sign(certificate);
        return certificate;
    }

    [Theory]
    [InlineData(CertificateStatus.Approved, VerificationStatus.Valid)]
    [InlineData(CertificateStatus.Revoked, VerificationStatus.Revoked)]
    [InlineData(CertificateStatus.Draft, VerificationStatus.NotIssued)]
    [InlineData(CertificateStatus.Rejected, VerificationStatus.NotIssued)]
    public void Status_is_mapped_for_intact_certificates(CertificateStatus status, VerificationStatus expected)
    {
        Assert.Equal(expected, CertificateVerificationService.Evaluate(Signed(status), Signer));
    }

    [Theory]
    [InlineData(CertificateStatus.Approved)]
    [InlineData(CertificateStatus.Revoked)]
    public void Tampering_wins_over_any_status(CertificateStatus status)
    {
        var certificate = Signed(status);
        certificate.Name = "Forged Name";

        Assert.Equal(VerificationStatus.Tampered, CertificateVerificationService.Evaluate(certificate, Signer));
    }
}
