using QRCertificateSystem.Models;
using QRCertificateSystem.Services;
using Xunit;

namespace QRCertificationSystem.Tests;

public class CertificateSignerTests
{
    private const string Key = "unit-test-signing-key-that-is-long-enough-1234";

    private static Certificate NewCertificate() => new()
    {
        CertificateId = "CERT-2026-ABCDEF123456",
        Name = "Ada Lovelace",
        Department = "Computer Engineering",
        Institution = "Düzce Üniversitesi",
        Degree = "Bachelor",
        GPA = 3.47m,
        IssueDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
        UniversityId = 1,
        Status = CertificateStatus.Approved
    };

    [Fact]
    public void Signed_certificate_is_valid()
    {
        var signer = new CertificateSigner(Key);
        var certificate = NewCertificate();
        certificate.HashValue = signer.Sign(certificate);

        Assert.True(signer.IsValid(certificate));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("gpa")]
    [InlineData("date")]
    [InlineData("institution")]
    [InlineData("university")]
    public void Changing_any_signed_field_invalidates_the_signature(string field)
    {
        var signer = new CertificateSigner(Key);
        var certificate = NewCertificate();
        certificate.HashValue = signer.Sign(certificate);

        switch (field)
        {
            case "name": certificate.Name = "Someone Else"; break;
            case "gpa": certificate.GPA = 4.00m; break;
            case "date": certificate.IssueDate = certificate.IssueDate.AddDays(1); break;
            case "institution": certificate.Institution = "Another University"; break;
            case "university": certificate.UniversityId = 2; break;
        }

        Assert.False(signer.IsValid(certificate));
    }

    [Fact]
    public void Signature_from_a_different_key_is_rejected()
    {
        var certificate = NewCertificate();
        certificate.HashValue = new CertificateSigner(Key + "-other").Sign(certificate);

        Assert.False(new CertificateSigner(Key).IsValid(certificate));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-hex")]
    [InlineData("ABCD")]
    public void Malformed_stored_signature_is_rejected(string stored)
    {
        var certificate = NewCertificate();
        certificate.HashValue = stored;

        Assert.False(new CertificateSigner(Key).IsValid(certificate));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too-short")]
    public void Missing_or_short_key_fails_fast(string? key)
    {
        Assert.Throws<InvalidOperationException>(() => new CertificateSigner(key));
    }

    [Fact]
    public void Payload_does_not_depend_on_the_current_culture()
    {
        var certificate = NewCertificate();
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            var us = CertificateSigner.BuildPayload(certificate);
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
            var tr = CertificateSigner.BuildPayload(certificate);

            Assert.Equal(us, tr);
            Assert.Contains("3.47", tr);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
        }
    }
}
