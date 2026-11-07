using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using QRCertificateSystem.Models;

namespace QRCertificateSystem.Services
{
    /// <summary>
    /// Produces and checks an HMAC-SHA256 signature over the content of a certificate.
    /// The key never leaves the server, so a certificate whose data was changed directly
    /// in the database (or forged) cannot carry a valid signature.
    /// </summary>
    public sealed class CertificateSigner
    {
        public const string ConfigKey = "CertificateSigning:Key";
        private const int MinimumKeyLength = 32;

        private readonly byte[] _key;

        public CertificateSigner(IConfiguration configuration) : this(configuration[ConfigKey])
        {
        }

        internal CertificateSigner(string? key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length < MinimumKeyLength)
            {
                throw new InvalidOperationException(
                    $"'{ConfigKey}' must be configured with at least {MinimumKeyLength} characters. " +
                    $"Use 'dotnet user-secrets set \"{ConfigKey}\" \"<random value>\"' locally " +
                    "or the CertificateSigning__Key environment variable in production.");
            }

            _key = Encoding.UTF8.GetBytes(key);
        }

        public string Sign(Certificate certificate)
        {
            var payload = Encoding.UTF8.GetBytes(BuildPayload(certificate));
            return Convert.ToHexString(HMACSHA256.HashData(_key, payload));
        }

        public bool IsValid(Certificate certificate)
        {
            if (string.IsNullOrWhiteSpace(certificate.HashValue))
            {
                return false;
            }

            byte[] stored;
            try
            {
                stored = Convert.FromHexString(certificate.HashValue);
            }
            catch (FormatException)
            {
                return false;
            }

            var expected = Convert.FromHexString(Sign(certificate));
            return CryptographicOperations.FixedTimeEquals(stored, expected);
        }

        /// <summary>
        /// Canonical, culture independent representation of the signed fields.
        /// Changing this format invalidates existing signatures, hence the version prefix.
        /// </summary>
        internal static string BuildPayload(Certificate c) => string.Join('\n',
            "v1",
            c.CertificateId,
            c.Name,
            c.Department,
            c.Institution,
            c.Degree ?? string.Empty,
            c.GPA?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty,
            c.IssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            c.UniversityId.ToString(CultureInfo.InvariantCulture));
    }
}
