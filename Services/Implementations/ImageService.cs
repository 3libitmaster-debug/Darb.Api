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
        // حصر الامتدادات المسموح بها للصور فقط
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".bmp" };

        public ImageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string?> SaveImageAsync(IFormFile? file, string folderName)
        {
            // التحقق من وجود الملف
            if (file == null || file.Length == 0) return null;

            // 1. التحقق من نوع الملف (صور فقط)
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!_allowedImageExtensions.Contains(extension))
            {
                return null; // سيتم رفض أي ملف ليس صورة
            }

            try
            {
                string webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string uploadsFolder = Path.Combine(webRootPath, "uploads", folderName);

                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                // 2. تحويل الامتداد دائماً إلى .webp
                string fileName = $"{Guid.NewGuid()}.webp";
                string filePath = Path.Combine(uploadsFolder, fileName);

                // 3. عملية التحويل (Optimization to WebP)
                using (var image = await Image.LoadAsync(file.OpenReadStream()))
                {
                    // حفظ الصورة بصيغة WebP مع جودة 75% (توازن مثالي بين الحجم والوضوح)
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
    }
}