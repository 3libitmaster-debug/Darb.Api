namespace Darb.Api.Services.Interfaces
{
    public interface IImageService
    {
        Task<string?> SaveImageAsync(IFormFile file, string folderName);
    }
}
 