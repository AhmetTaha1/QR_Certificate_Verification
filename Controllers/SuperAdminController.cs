using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCertificateSystem.Data;
using QRCertificateSystem.Models;

namespace QRCertificateSystem.Controllers
{
    /// <summary>Platform owner: approves the universities that registered themselves.</summary>
    [Authorize(Roles = nameof(UserRole.SuperAdmin))]
    public class SuperAdminController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUniversities = await _context.Universities.CountAsync();
            ViewBag.PendingUniversities = await _context.Universities.CountAsync(u => !u.IsActive);
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalCertificates = await _context.Certificates.CountAsync();
            ViewBag.TotalVerifications = await _context.VerificationLogs.CountAsync();
            return View();
        }

        public async Task<IActionResult> Universities()
        {
            var universities = await _context.Universities
                .AsNoTracking()
                .Include(u => u.Users.Where(x => x.Role == UserRole.UniversityAdmin))
                .OrderBy(u => u.IsActive)
                .ThenByDescending(u => u.CreatedAt)
                .ToListAsync();

            ViewBag.CertificateCounts = await _context.Certificates
                .GroupBy(c => c.UniversityId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            return View(universities);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUniversity(int id)
        {
            var university = await _context.Universities
                .Include(u => u.Users)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (university == null)
            {
                return NotFound();
            }

            university.IsActive = true;
            university.UpdatedAt = DateTime.UtcNow;

            foreach (var admin in university.Users.Where(u => u.Role == UserRole.UniversityAdmin))
            {
                admin.IsActive = true;
                admin.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"University '{university.Name}' has been approved.";
            return RedirectToAction(nameof(Universities));
        }

        [HttpPost]
        public async Task<IActionResult> RejectUniversity(int id)
        {
            var university = await _context.Universities
                .Include(u => u.Users)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (university == null)
            {
                return NotFound();
            }

            // Only pending registrations can be rejected; an active university may already have issued certificates.
            if (university.IsActive)
            {
                TempData["Error"] = "Only pending registrations can be rejected.";
                return RedirectToAction(nameof(Universities));
            }

            _context.Users.RemoveRange(university.Users);
            _context.Universities.Remove(university);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Registration of '{university.Name}' has been rejected.";
            return RedirectToAction(nameof(Universities));
        }
    }
}
