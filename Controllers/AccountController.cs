using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCertificateSystem.Data;
using QRCertificateSystem.Dtos;
using QRCertificateSystem.Models;
using QRCertificateSystem.Services;

namespace QRCertificateSystem.Controllers
{
    public class AccountController(ApplicationDbContext context, ILogger<AccountController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<AccountController> _logger = logger;

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToHome(User.FindFirstValue(ClaimTypes.Role));
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequest model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            var email = NormalizeEmail(model.Email);
            var user = await _context.Users
                .Include(u => u.University)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            if (!user.IsActive || (user.Role == UserRole.UniversityAdmin && user.University?.IsActive != true))
            {
                ViewBag.Error = "Your account is waiting for approval.";
                return View();
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString())
            };
            if (user.UniversityId.HasValue)
            {
                claims.Add(new Claim(AppClaimTypes.UniversityId, user.UniversityId.Value.ToString()));
            }

            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = model.RememberMe });

            _logger.LogInformation("User {UserId} signed in", user.Id);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToHome(user.Role.ToString());
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            await LoadActiveUniversitiesAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterStudentRequest model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = FirstModelError();
                await LoadActiveUniversitiesAsync();
                return View(model);
            }

            var email = NormalizeEmail(model.Email);
            var studentNumber = model.StudentNumber.Trim();

            string? error = null;
            if (!await _context.Universities.AnyAsync(u => u.Id == model.UniversityId && u.IsActive))
            {
                error = "Please select a valid university.";
            }
            else if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                error = "This email is already registered.";
            }
            else if (await _context.Students.AnyAsync(s =>
                         s.UniversityId == model.UniversityId && s.StudentNumber == studentNumber))
            {
                error = "This student number is already registered at the selected university.";
            }

            if (error != null)
            {
                ViewBag.Error = error;
                await LoadActiveUniversitiesAsync();
                return View(model);
            }

            var user = new User
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = UserRole.Student,
                UniversityId = model.UniversityId,
                IsActive = true,
                Student = new Student
                {
                    UniversityId = model.UniversityId,
                    StudentNumber = studentNumber,
                    EnrollmentYear = DateTime.UtcNow.Year,
                    IsActive = true
                }
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Registration successful! You can now sign in.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult RegisterUniversity() => View();

        [HttpPost]
        public async Task<IActionResult> RegisterUniversity(RegisterUniversityRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = NormalizeEmail(model.Email);
            var universityName = model.UniversityName.Trim();

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email already exists.");
                return View(model);
            }

            if (await _context.Universities.AnyAsync(u => u.Name == universityName))
            {
                ModelState.AddModelError(nameof(model.UniversityName), "University already exists.");
                return View(model);
            }

            var university = new University
            {
                Name = universityName,
                ShortName = model.ShortName.Trim(),
                Website = model.Website,
                City = model.City,
                IsActive = false // Waits for SuperAdmin approval
            };

            university.Users.Add(new User
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                PhoneNumber = model.PhoneNumber,
                Role = UserRole.UniversityAdmin,
                IsActive = false // Activated together with the university
            });

            // A single SaveChanges call runs in one transaction.
            _context.Universities.Add(university);
            await _context.SaveChangesAsync();

            return View("RegistrationSuccess");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.GetUserId();
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.University)
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return await Logout();
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied() => View();

        private RedirectToActionResult RedirectToHome(string? role) => role switch
        {
            nameof(UserRole.SuperAdmin) => RedirectToAction("Index", "SuperAdmin"),
            nameof(UserRole.UniversityAdmin) => RedirectToAction("Index", "Admin"),
            nameof(UserRole.Student) => RedirectToAction("MyCertificates", "Student"),
            _ => RedirectToAction("Index", "Home")
        };

        private async Task LoadActiveUniversitiesAsync()
        {
            ViewBag.Universities = await _context.Universities
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        private string FirstModelError() =>
            ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
            ?? "Please check the form.";

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    }
}
