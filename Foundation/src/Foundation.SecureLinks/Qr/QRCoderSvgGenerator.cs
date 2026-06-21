using System.Text;
using Foundation.SecureLinks.Abstractions;
using Foundation.SecureLinks.Models;
using QRCoder;

namespace Foundation.SecureLinks.Qr;

public sealed class QRCoderSvgGenerator : IQrArtifactGenerator
{
    public Task<QrArtifact> GenerateSvgAsync(string payload, string fileName, CancellationToken cancellationToken = default)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var data = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var svg = new SvgQRCode(data).GetGraphic(12);
        var bytes = Encoding.UTF8.GetBytes(svg);
        var dataUri = $"data:image/svg+xml;base64,{Convert.ToBase64String(bytes)}";

        return Task.FromResult(new QrArtifact("image/svg+xml", $"{fileName}.svg", bytes, dataUri));
    }
}
