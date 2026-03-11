namespace Darb.Api.Services.Interfaces
{
    public interface IImageService
    {
        Task<string?> SaveImageAsync(IFormFile file, string folderName);
        Task<string?> UpdateImageAsync(IFormFile? newFile, string? oldImagePath, string folderName);
        void DeleteImage(string? imagePath);
    }
}