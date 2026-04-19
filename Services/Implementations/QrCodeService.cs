using Darb.Api.Services.Interfaces;
using QRCoder;
using System;

namespace Darb.Api.Services.Implementations
{
    public class QrCodeService : IQrCodeService
    {
        public string GenerateQrCodeBase64(string payload)
        {
            if (string.IsNullOrEmpty(payload))
                return string.Empty;

            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImage = qrCode.GetGraphic(20);
                    return Convert.ToBase64String(qrCodeImage);
                }
            }
        }
    }
}
