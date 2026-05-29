using Darb.Api.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace Darb.Api.Services.Implemention
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;
        // Õ’— «·«„ œ«œ«  «·„”„ÊÕ »Â« ··’Ê— ›ﬁÿ
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".bmp" };

        public ImageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string?> UpdateImageAsync(IFormFile? newFile, string? oldImagePath, string folderName)
        {
            if (newFile == null || newFile.Length == 0) return oldImagePath;

            // Delete the old image if it exists
            DeleteImage(oldImagePath);

            // Save the new image
            return await SaveImageAsync(newFile, folderName);
        }

        public async Task<string?> SaveImageAsync(IFormFile? file, string folderName)
        {
            // «· Õﬁﬁ „‰ ÊÃÊœ «·„·›
            if (file == null || file.Length == 0) return null;

            // 1. «· Õﬁﬁ „‰ ‰Ê⁄ «·„·› (’Ê— ›ﬁÿ)
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!_allowedImageExtensions.Contains(extension))
            {
                return null; // ”Ì „ —›÷ √Ì „·› ·Ì” ’Ê—…
            }

            try
            {
                string webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string uploadsFolder = Path.Combine(webRootPath, "uploads", folderName);

                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                // 2.  ÕÊÌ· «·«„ œ«œ œ«∆„« ≈·Ï .webp
                string fileName = $"{Guid.NewGuid()}.webp";
                string filePath = Path.Combine(uploadsFolder, fileName);

                // 3. ⁄„·Ì… «· ÕÊÌ· (Optimization to WebP)
                using (var image = await Image.LoadAsync(file.OpenReadStream()))
                {
                    // Õ›Ÿ «·’Ê—… »’Ì€… WebP „⁄ ÃÊœ… 75% ( Ê«“‰ „À«·Ì »Ì‰ «·ÕÃ„ Ê«·Ê÷ÊÕ)
                    await image.SaveAsync(filePath, new WebpEncoder { Quality = 75 });
                }

                return $"/uploads/{folderName}/{fileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageService Error]: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Physically deletes an image from the server's filesystem.
        /// </summary>
        /// <param name="imagePath">Relative path of the image stored in the DB.</param>
        public void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return;

            try
            {
                // Convert relative path to physical path
                string webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                // Remove leading slash if any to avoid Path.Combine issues
                string relativePath = imagePath.TrimStart('/');
                string fullPath = Path.Combine(webRootPath, relativePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    Console.WriteLine($"[ImageService]: Deleted image at {fullPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageService Delete Error]: {ex.Message}");
            }
        }
    }
}