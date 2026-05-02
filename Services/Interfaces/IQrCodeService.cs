using System;

namespace Darb.Api.Services.Interfaces
{
    public interface IQrCodeService
    {
        string GenerateQrCodeBase64(string payload);
    }
}
