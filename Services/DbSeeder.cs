using Microsoft.EntityFrameworkCore;
using QRCertificateSystem.Data;
using QRCertificateSystem.Models;

namespace QRCertificateSystem.Services
{
    /// <summary>
    /// Applies pending migrations and creates the demo accounts.
    /// Credentials come from configuration (user-secrets / environment), never from source code.
    /// </summary>
    public class DbSeeder(ApplicationDbContext context, IConfiguration configuration, ILogger<DbSeeder> logger)
    {
        private const int DemoUniversityId = 1;

        private readonly ApplicationDbContext _context = context;
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<DbSeeder> _logger = logger;

        public async Task SeedAsync()
        {
            await _context.Database.MigrateAsync();

            var password = _configuration["Seed:Password"];
            if (string.IsNullOrWhiteSpace(password))
            {
                if (!await _context.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
                {
                    _logger.LogWarning(
                        "No SuperAdmin exists and 'Seed:Password' is not configured, so no demo accounts were created.");
                }
                return;
            }

            if (password.Length < 8)
            {
                throw new InvalidOperationException("'Seed:Password' must be at least 8 characters long.");
            }

            await EnsureUserAsync(
                _configuration["Seed:SuperAdminEmail"] ?? "superadmin@demo.com",
                "Super", "Admin", UserRole.SuperAdmin, universityId: null, password);

            if (await _context.Universities.AnyAsync(u => u.Id == DemoUniversityId))
            {
                await EnsureUserAsync(
                    _configuration["Seed:UniversityAdminEmail"] ?? "admin@demo.com",
                    "University", "Admin", UserRole.UniversityAdmin, DemoUniversityId, password);
            }
        }

        private async Task EnsureUserAsync(string email, string firstName, string lastName,
            UserRole role, int? universityId, string password)
        {
            email = email.Trim().ToLowerInvariant();
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                return;
            }

            _context.Users.Add(new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                UniversityId = universityId,
                IsActive = true,
                EmailConfirmed = true
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created demo {Role} account {Email}", role, email);
        }
    }
}
