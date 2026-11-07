using Microsoft.AspNetCore.Mvc;
using QRCertificateSystem.Services;

namespace QRCertificateSystem.Controllers;

/// <summary>Public pages reached by scanning the QR code or typing a certificate ID.</summary>
public class VerificationController(CertificateVerificationService verificationService) : Controller
{
    private readonly CertificateVerificationService _verificationService = verificationService;

    [HttpGet("/verify")]
    public IActionResult Verify(string? certificateId)
    {
        if (!string.IsNullOrWhiteSpace(certificateId))
        {
            return RedirectToAction(nameof(Result), new { certificateId = certificateId.Trim() });
        }

        return View("VerifyPage");
    }

    [HttpGet("/verify/{certificateId}")]
    public async Task<IActionResult> Result(string certificateId, CancellationToken ct)
    {
        var result = await _verificationService.VerifyAsync(
            certificateId, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);

        return View("Index", result);
    }
}
