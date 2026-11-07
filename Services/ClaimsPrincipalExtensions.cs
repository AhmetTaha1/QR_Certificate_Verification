using System.Security.Claims;

namespace QRCertificateSystem.Services
{
    public static class AppClaimTypes
    {
        public const string UniversityId = "university_id";
    }

    public static class ClaimsPrincipalExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal user) =>
            int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        public static int? GetUniversityId(this ClaimsPrincipal user) =>
            int.TryParse(user.FindFirstValue(AppClaimTypes.UniversityId), out var id) ? id : null;
    }
}
