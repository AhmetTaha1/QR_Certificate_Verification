using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCertificateSystem.Data;
using QRCertificateSystem.Services;

namespace QRCertificateSystem.Controllers;

/// <summary>Small public JSON / image API used by the diploma page and third-party verifiers.</summary>
[ApiController]
[Route("api/certificate")]
public class CertificateController(ApplicationDbContext context, QRCodeService qrCodeService, CertificateVerificationService verificationService) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly QRCodeService _qrCodeService = qrCodeService;
    private readonly CertificateVerificationService _verificationService = verificationService;

    /// <summary>PNG QR code that points to the public verification page.</summary>
    [HttpGet("qrcode/{certificateId}")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> GetQRCode(string certificateId, CancellationToken ct)
    {
        var exists = await _context.Certificates.AnyAsync(c => c.CertificateId == certificateId, ct);
        if (!exists)
        {
            return NotFound();
        }

        var url = _qrCodeService.BuildVerificationUrl(Request, certificateId);
        return File(_qrCodeService.GenerateQRCode(url), "image/png");
    }

    /// <summary>Machine-readable verification result.</summary>
    [HttpGet("verify/{certificateId}")]
    public async Task<IActionResult> Verify(string certificateId, CancellationToken ct)
    {
        var result = await _verificationService.VerifyAsync(
            certificateId, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);

        if (result.Certificate == null)
        {
            return NotFound(new { certificateId = result.CertificateId, status = result.Status.ToString() });
        }

        var c = result.Certificate;
        return Ok(new
        {
            certificateId = c.CertificateId,
            status = result.Status.ToString(),
            valid = result.IsValid,
            // Never echo data whose signature does not match: it may have been forged.
            certificate = result.Status == VerificationStatus.Tampered ? null : new
            {
                name = c.Name,
                institution = c.Institution,
                department = c.Department,
                degree = c.Degree,
                gpa = c.GPA,
                issueDate = c.IssueDate.ToString("yyyy-MM-dd")
            }
        });
    }
}
