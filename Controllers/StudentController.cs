using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCertificateSystem.Data;
using QRCertificateSystem.Models;
using QRCertificateSystem.Services;

namespace QRCertificateSystem.Controllers
{
    [Authorize(Roles = nameof(UserRole.Student))]
    public class StudentController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        private IQueryable<Certificate> OwnCertificates
        {
            get
            {
                var userId = User.GetUserId();
                return _context.Certificates
                    .AsNoTracking()
                    .Include(c => c.University)
                    .Where(c => c.Student != null && c.Student.UserId == userId);
            }
        }

        public IActionResult Index() => RedirectToAction(nameof(MyCertificates));

        public async Task<IActionResult> MyCertificates()
        {
            var certificates = await OwnCertificates
                .OrderByDescending(c => c.IssueDate)
                .ToListAsync();

            return View(certificates);
        }

        public async Task<IActionResult> Diploma(int id)
        {
            var certificate = await OwnCertificates.FirstOrDefaultAsync(c => c.Id == id);

            return certificate == null
                ? NotFound()
                : View("~/Views/Shared/DiplomaTemplate.cshtml", certificate);
        }
    }
}
