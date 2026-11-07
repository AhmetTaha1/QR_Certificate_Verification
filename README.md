# QR Certificate Verification

A multi-tenant ASP.NET Core MVC application in which universities issue **digitally signed certificates**, students keep them in their account, and anyone can check whether a certificate is genuine by scanning the QR code printed on it.

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4) ![PostgreSQL](https://img.shields.io/badge/PostgreSQL-EF%20Core-336791)

## Features

- **Three roles**
  - **Super Admin**: approves or rejects universities that apply to the platform.
  - **University Admin**: issues, edits, revokes and deletes certificates for their own university only.
  - **Student**: registers with a student number and sees the certificates linked to them.
- **Tamper detection**: every certificate is signed with HMAC-SHA256 using a server-side secret. If someone changes a row directly in the database (for example the GPA or the name), the verification page reports the certificate as *invalid*.
- **Public verification** at `/verify/{certificateId}`, with five possible results: *valid*, *revoked*, *not issued*, *invalid signature* and *not found*. Every lookup is recorded in a verification log.
- **QR code and PDF**: the diploma page includes a QR code that points to the verification page, and you can download the diploma as a PDF.
- **JSON API** for third-party verifiers: `GET /api/certificate/verify/{id}`.

## How verification works

```
Issue:   payload = id | name | department | institution | degree | gpa | date | universityId
         signature = HMAC-SHA256(serverKey, payload)  → stored with the certificate

Verify:  recompute HMAC from the stored fields
         ├─ mismatch            → INVALID (data was altered or forged)
         ├─ status = Revoked    → REVOKED
         ├─ status ≠ Approved   → NOT ISSUED
         └─ otherwise           → VALID
```

Certificate IDs contain 48 random bits (`CERT-2026-1A2B3C4D5E6F`), so nobody can guess or enumerate them.

## Security notes

- Cookie authentication with role-based authorization (`[Authorize(Roles = ...)]`). Every query that reads or changes a certificate is filtered by the admin's university.
- Global antiforgery validation on every POST. Logout is a POST too.
- Passwords are hashed with BCrypt.
- No secrets in the repository. The connection string, signing key and demo password come from user-secrets or environment variables, and the app refuses to start if the signing key is missing.
- The public verification page shows only data about the certificate itself. It never shows the student's email address, and it hides all details when the signature check fails.

## Tech stack

ASP.NET Core 9 MVC · Entity Framework Core 9 · PostgreSQL (Npgsql) · BCrypt.Net · QRCoder · Bootstrap 5 · html2canvas + jsPDF · xUnit · GitHub Actions

## Getting started

Prerequisites: .NET 9 SDK and a running PostgreSQL server.

```bash
git clone <repo-url>
cd QR_Certificate_Verification

# 1. Configuration (stored outside the repository)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=qrcertificatedb;Username=postgres;Password=postgres"
dotnet user-secrets set "CertificateSigning:Key" "$(openssl rand -base64 48)"
dotnet user-secrets set "Seed:Password" "choose-a-demo-password"

# 2. Run: migrations are applied and the demo accounts are created on startup
dotnet run --launch-profile http

# or with hot reload during development
dotnet watch run --launch-profile http
```

Open <http://localhost:5183>.

| Demo account | Email | Password |
|---|---|---|
| Super Admin | `superadmin@demo.local` | value of `Seed:Password` |
| University Admin (Düzce Üniversitesi) | `admin@duzce.demo.local` | value of `Seed:Password` |

Students sign up at `/Account/Register`. New universities apply at `/Account/RegisterUniversity` and appear in the Super Admin panel for approval.

### Configuration reference

| Key | Required | Description |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | yes | PostgreSQL connection string |
| `CertificateSigning:Key` | yes | HMAC key, at least 32 characters. Changing it invalidates all existing signatures. |
| `Seed:Password` | no | Creates the demo accounts if they do not exist |
| `App:PublicBaseUrl` | no | Base URL used in QR codes (for example `https://certs.example.com`). Defaults to the current request host. |

In production, set the same keys as environment variables, for example `CertificateSigning__Key` and `ConnectionStrings__DefaultConnection`.

## Tests

```bash
dotnet test
```

The unit tests cover signing and tamper detection: every signed field, a wrong key, a malformed signature, culture independence (tr-TR vs en-US) and the verification status rules. CI runs the build and the tests on every push.

## Project structure

```
Controllers/   Account, Admin (university), SuperAdmin, Student, Verification, Certificate (API)
Services/      CertificateSigner, CertificateVerificationService, QRCodeService, DbSeeder
Models/        User, University, Student, Certificate, VerificationLog
Views/         Razor views (one standalone page per screen)
Migrations/    EF Core migrations
tests/         xUnit tests
```
