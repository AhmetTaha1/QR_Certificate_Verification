using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCertificateSystem.Data;
using QRCertificateSystem.Models;

namespace QRCertificateSystem.Controllers
{
    public class HomeController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveCertificates = await _context.Certificates.CountAsync(c => c.Status == CertificateStatus.Approved);
            ViewBag.Universities = await _context.Universities.CountAsync(u => u.IsActive);
            ViewBag.Verifications = await _context.VerificationLogs.CountAsync();
            return View();
        }

        public IActionResult Error() => View();
    }
}
