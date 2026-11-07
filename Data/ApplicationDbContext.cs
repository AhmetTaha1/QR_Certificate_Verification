namespace QRCertificateSystem.Data;

using Microsoft.EntityFrameworkCore;
using QRCertificateSystem.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    public DbSet<User> Users { get; set; }
    public DbSet<University> Universities { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Certificate> Certificates { get; set; }
    public DbSet<VerificationLog> VerificationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User -> University
        modelBuilder.Entity<User>()
            .HasOne(u => u.University)
            .WithMany(uni => uni.Users)
            .HasForeignKey(u => u.UniversityId)
            .OnDelete(DeleteBehavior.SetNull);

        // Student -> User (One-to-One)
        modelBuilder.Entity<Student>()
            .HasOne(s => s.User)
            .WithOne(u => u.Student)
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Student -> University
        modelBuilder.Entity<Student>()
            .HasOne(s => s.University)
            .WithMany(uni => uni.Students)
            .HasForeignKey(s => s.UniversityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Certificate -> Student
        modelBuilder.Entity<Certificate>()
            .HasOne(c => c.Student)
            .WithMany(s => s.Certificates)
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Certificate -> University
        modelBuilder.Entity<Certificate>()
            .HasOne(c => c.University)
            .WithMany(uni => uni.Certificates)
            .HasForeignKey(c => c.UniversityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Certificate -> CreatedByUser
        modelBuilder.Entity<Certificate>()
            .HasOne(c => c.CreatedByUser)
            .WithMany(u => u.CreatedCertificates)
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // VerificationLog -> Certificate
        modelBuilder.Entity<VerificationLog>()
            .HasOne(vl => vl.Certificate)
            .WithMany(c => c.VerificationLogs)
            .HasForeignKey(vl => vl.CertificateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Student>()
            .HasIndex(s => new { s.StudentNumber, s.UniversityId })
            .IsUnique();

        modelBuilder.Entity<Certificate>()
            .HasIndex(c => c.CertificateId)
            .IsUnique();

        // Seed Data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Universities
        modelBuilder.Entity<University>().HasData(
            new University
            {
                Id = 1,
                Name = "Düzce Üniversitesi",
                ShortName = "DÜ",
                City = "Düzce",
                IsActive = true,
                PrimaryColor = "#1e3a8a",
                SecondaryColor = "#3b82f6",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new University
            {
                Id = 2,
                Name = "Sakarya Üniversitesi",
                ShortName = "SAÜ",
                City = "Sakarya",
                IsActive = true,
                PrimaryColor = "#dc2626",
                SecondaryColor = "#ef4444",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new University
            {
                Id = 3,
                Name = "Kocaeli Üniversitesi",
                ShortName = "KOÜ",
                City = "Kocaeli",
                IsActive = true,
                PrimaryColor = "#059669",
                SecondaryColor = "#10b981",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Users are not seeded here: password hashes must not live in source code / migrations.
        // Demo accounts are created at startup by DbSeeder from configuration.
    }
}