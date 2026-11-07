using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCertificateSystem.Data;
using QRCertificateSystem.Dtos;
using QRCertificateSystem.Models;
using QRCertificateSystem.Services;

namespace QRCertificateSystem.Controllers
{
    /// <summary>Certificate management for the admin of a single university.</summary>
    [Authorize(Roles = nameof(UserRole.UniversityAdmin))]
    public class AdminController(ApplicationDbContext context, CertificateSigner signer, QRCodeService qrCodeService, ILogger<AdminController> logger) : Controller
    {
        private const int PageSize = 10;

        private readonly ApplicationDbContext _context = context;
        private readonly CertificateSigner _signer = signer;
        private readonly QRCodeService _qrCodeService = qrCodeService;
        private readonly ILogger<AdminController> _logger = logger;

        private int UniversityId => User.GetUniversityId()
            ?? throw new InvalidOperationException("University admin without a university claim.");

        private IQueryable<Certificate> OwnCertificates =>
            _context.Certificates.Where(c => c.UniversityId == UniversityId);

        public async Task<IActionResult> Index(string? searchTerm, string sortBy = "date", int page = 1)
        {
            var query = OwnCertificates
                .AsNoTracking()
                .Include(c => c.Student)
                    .ThenInclude(s => s!.User)
                .AsQueryable();

            searchTerm = searchTerm?.Trim();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var pattern = $"%{searchTerm}%";
                query = query.Where(c =>
                    EF.Functions.ILike(c.Name, pattern) ||
                    EF.Functions.ILike(c.CertificateId, pattern) ||
                    EF.Functions.ILike(c.Department, pattern) ||
                    (c.Student != null &&
                     (EF.Functions.ILike(c.Student.StudentNumber, pattern) ||
                      EF.Functions.ILike(c.Student.User.Email, pattern))));
            }

            query = sortBy switch
            {
                "name" => query.OrderBy(c => c.Name),
                "issuedate" => query.OrderByDescending(c => c.IssueDate),
                "gpa" => query.OrderByDescending(c => c.GPA ?? 0),
                _ => query.OrderByDescending(c => c.CreatedAt)
            };

            var total = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            page = Math.Clamp(page, 1, totalPages);

            ViewBag.Certificates = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
            ViewBag.Students = await LoadStudentsAsync();
            ViewBag.UniversityName = await UniversityNameAsync();
            ViewBag.TotalCertificates = total;
            ViewBag.RevokedCertificates = await OwnCertificates.CountAsync(c => c.Status == CertificateStatus.Revoked);
            ViewBag.TotalVerifications = await _context.VerificationLogs
                .CountAsync(v => v.Certificate!.UniversityId == UniversityId);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchTerm = searchTerm ?? string.Empty;
            ViewBag.SortBy = sortBy;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadFormDataAsync();
            return View(new CertificateFormModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CertificateFormModel form)
        {
            var university = await _context.Universities.FindAsync(UniversityId);
            if (university == null)
            {
                return Forbid();
            }

            var certificate = new Certificate
            {
                CertificateId = NewCertificateId(),
                UniversityId = university.Id,
                Status = CertificateStatus.Approved,
                CreatedByUserId = User.GetUserId()
            };

            var error = await ApplyFormAsync(form, certificate, university);
            if (error != null)
            {
                TempData["Error"] = error;
                await LoadFormDataAsync();
                return View(form);
            }

            certificate.QRCodeData = _qrCodeService.BuildVerificationUrl(Request, certificate.CertificateId);
            certificate.HashValue = _signer.Sign(certificate);

            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Certificate {CertificateId} issued by user {UserId}",
                certificate.CertificateId, User.GetUserId());

            TempData["Success"] = $"Certificate created successfully. ID: {certificate.CertificateId}";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var certificate = await OwnCertificates.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (certificate == null)
            {
                return NotFound();
            }

            ViewBag.Certificate = certificate;
            await LoadFormDataAsync();
            return View(new CertificateFormModel
            {
                StudentId = certificate.StudentId,
                StudentName = certificate.Name,
                Department = certificate.Department,
                Degree = certificate.Degree ?? string.Empty,
                IssueDate = certificate.IssueDate,
                Gpa = certificate.GPA?.ToString("0.00", CultureInfo.InvariantCulture),
                Notes = certificate.Notes
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, CertificateFormModel form)
        {
            var certificate = await OwnCertificates.Include(c => c.University).FirstOrDefaultAsync(c => c.Id == id);
            if (certificate == null)
            {
                return NotFound();
            }

            var error = await ApplyFormAsync(form, certificate, certificate.University);
            if (error != null)
            {
                TempData["Error"] = error;
                ViewBag.Certificate = certificate;
                await LoadFormDataAsync();
                return View(form);
            }

            certificate.HashValue = _signer.Sign(certificate);
            certificate.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Certificate updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public Task<IActionResult> Revoke(int id) => SetStatusAsync(id, CertificateStatus.Revoked, "revoked");

        [HttpPost]
        public Task<IActionResult> Reinstate(int id) => SetStatusAsync(id, CertificateStatus.Approved, "reinstated");

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var certificate = await OwnCertificates.FirstOrDefaultAsync(c => c.Id == id);
            if (certificate == null)
            {
                return NotFound();
            }

            _context.Certificates.Remove(certificate);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Certificate {certificate.CertificateId} deleted.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Diploma(int id)
        {
            var certificate = await OwnCertificates
                .AsNoTracking()
                .Include(c => c.University)
                .FirstOrDefaultAsync(c => c.Id == id);

            return certificate == null
                ? NotFound()
                : View("~/Views/Shared/DiplomaTemplate.cshtml", certificate);
        }

        private async Task<IActionResult> SetStatusAsync(int id, CertificateStatus status, string verb)
        {
            var certificate = await OwnCertificates.FirstOrDefaultAsync(c => c.Id == id);
            if (certificate == null)
            {
                return NotFound();
            }

            certificate.Status = status;
            certificate.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Certificate {certificate.CertificateId} {verb}.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Validates the form and copies it onto the certificate. Returns an error message or null.</summary>
        private async Task<string?> ApplyFormAsync(CertificateFormModel form, Certificate certificate, University university)
        {
            if (!ModelState.IsValid)
            {
                return "Please fill in all required fields.";
            }

            if (!CertificateFormModel.AllowedDegrees.Contains(form.Degree))
            {
                return "Please select a valid degree.";
            }

            var issueDate = DateTime.SpecifyKind(form.IssueDate.Date, DateTimeKind.Utc);
            if (issueDate > DateTime.UtcNow.Date.AddDays(1))
            {
                return "Issue date cannot be in the future.";
            }

            decimal? gpa = null;
            if (!string.IsNullOrWhiteSpace(form.Gpa))
            {
                if (!decimal.TryParse(form.Gpa.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                    || parsed < 0 || parsed > 4)
                {
                    return "GPA must be a number between 0.00 and 4.00.";
                }
                gpa = Math.Round(parsed, 2);
            }

            Student? student = null;
            if (form.StudentId.HasValue)
            {
                student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == form.StudentId.Value && s.UniversityId == university.Id);
                if (student == null)
                {
                    return "The selected student does not belong to your university.";
                }
            }

            var name = string.IsNullOrWhiteSpace(form.StudentName) ? student?.User.FullName : form.StudentName.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Student name is required when no registered student is selected.";
            }

            certificate.StudentId = student?.Id;
            certificate.Name = name;
            certificate.Department = form.Department.Trim();
            certificate.Institution = university.Name;
            certificate.Degree = form.Degree;
            certificate.IssueDate = issueDate;
            certificate.GPA = gpa;
            certificate.Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim();
            return null;
        }

        private async Task LoadFormDataAsync()
        {
            ViewBag.Students = await LoadStudentsAsync();
            ViewBag.UniversityName = await UniversityNameAsync();
        }

        private Task<List<Student>> LoadStudentsAsync() => _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.UniversityId == UniversityId)
            .OrderBy(s => s.User.FirstName).ThenBy(s => s.User.LastName)
            .ToListAsync();

        private Task<string?> UniversityNameAsync() => _context.Universities
            .Where(u => u.Id == UniversityId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync();

        /// <summary>48 random bits: practically impossible to guess or enumerate.</summary>
        private static string NewCertificateId() =>
            $"CERT-{DateTime.UtcNow.Year}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}";
    }
}
