using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.Helpers
{
    public static class FileUploadHelper
    {
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt" };
        private static readonly long MaxImageSize = 5 * 1024 * 1024; // 5MB
        private static readonly long MaxDocumentSize = 50 * 1024 * 1024; // 50MB

        public static class FileValidation
        {
            public static ValidationResult ValidateImageFile(IFormFile file)
            {
                if (file == null || file.Length == 0)
                {
                    return new ValidationResult("File is required");
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(extension))
                {
                    return new ValidationResult($"Only image files are allowed: {string.Join(", ", AllowedImageExtensions)}");
                }

                if (file.Length > MaxImageSize)
                {
                    return new ValidationResult($"File size cannot exceed {MaxImageSize / (1024 * 1024)}MB");
                }

                return ValidationResult.Success!;
            }

            public static ValidationResult ValidateDocumentFile(IFormFile file)
            {
                if (file == null || file.Length == 0)
                {
                    return new ValidationResult("File is required");
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedDocumentExtensions.Contains(extension))
                {
                    return new ValidationResult($"Only document files are allowed: {string.Join(", ", AllowedDocumentExtensions)}");
                }

                if (file.Length > MaxDocumentSize)
                {
                    return new ValidationResult($"File size cannot exceed {MaxDocumentSize / (1024 * 1024)}MB");
                }

                return ValidationResult.Success!;
            }

            public static bool IsImageFile(IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                return AllowedImageExtensions.Contains(extension);
            }

            public static bool IsDocumentFile(IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                return AllowedDocumentExtensions.Contains(extension);
            }
        }

        public static class FileUtils
        {
            public static string GenerateUniqueFileName(string originalFileName)
            {
                var extension = Path.GetExtension(originalFileName);
                var fileName = Path.GetFileNameWithoutExtension(originalFileName);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var guid = Guid.NewGuid().ToString("N")[..8];

                return $"{fileName}_{timestamp}_{guid}{extension}";
            }

            public static async Task<string> SaveFileAsync(IFormFile file, string uploadPath, string? customFileName = null)
            {
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var fileName = customFileName ?? GenerateUniqueFileName(file.FileName);
                var filePath = Path.Combine(uploadPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                return fileName;
            }

            public static void DeleteFile(string filePath)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception)
                {
                    // Log error but don't throw - file deletion is not critical
                }
            }

            public static string GetMimeType(string fileName)
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                return extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    ".pdf" => "application/pdf",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".xls" => "application/vnd.ms-excel",
                    ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ".txt" => "text/plain",
                    _ => "application/octet-stream"
                };
            }
        }
    }

    // Custom validation attributes
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly long _maxFileSize;

        public MaxFileSizeAttribute(long maxFileSize)
        {
            _maxFileSize = maxFileSize;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                if (file.Length > _maxFileSize)
                {
                    return new ValidationResult($"File size cannot exceed {_maxFileSize / (1024 * 1024)}MB");
                }
            }

            return ValidationResult.Success;
        }
    }

    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;

        public AllowedExtensionsAttribute(params string[] extensions)
        {
            _extensions = extensions;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_extensions.Contains(extension))
                {
                    return new ValidationResult($"Only the following file types are allowed: {string.Join(", ", _extensions)}");
                }
            }

            return ValidationResult.Success;
        }
    }
}