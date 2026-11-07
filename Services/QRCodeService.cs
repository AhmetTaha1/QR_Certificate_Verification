using QRCoder;

namespace QRCertificateSystem.Services
{
    public class QRCodeService(IConfiguration configuration)
    {
        private readonly string? _publicBaseUrl = configuration["App:PublicBaseUrl"]?.TrimEnd('/');

        public byte[] GenerateQRCode(string text)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(10);
        }

        /// <summary>
        /// Builds the public verification URL. "App:PublicBaseUrl" wins over the current request
        /// so that QR codes generated behind a proxy or on localhost still point to the real domain.
        /// </summary>
        public string BuildVerificationUrl(HttpRequest request, string certificateId)
        {
            var baseUrl = string.IsNullOrWhiteSpace(_publicBaseUrl)
                ? $"{request.Scheme}://{request.Host}{request.PathBase}"
                : _publicBaseUrl;

            return $"{baseUrl}/verify/{Uri.EscapeDataString(certificateId)}";
        }
    }
}
