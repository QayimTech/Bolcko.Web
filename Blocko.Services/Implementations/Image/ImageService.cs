using Amazon.S3;
using Amazon.S3.Model;
using Blocko.Services.Interfaces.Image;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blocko.Services.Implementations.Images
{
    /// <summary>
    /// Saves product/variant images as compressed WebP.
    /// - If ImageSettings:S3BucketName is set  -> uploads to AWS S3 (production / ECS).
    /// - Otherwise                              -> saves under wwwroot on local disk (local development).
    /// The value returned to callers is always the same relative path
    /// (e.g. "images/products/abc.webp"), which is also the S3 object key.
    /// </summary>
    public class ImageService : IImageService
    {
        private readonly string _basePath;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ImageService> _logger;
        private readonly IAmazonS3? _s3;
        private readonly string _bucket;
        private readonly string _keyPrefix;

        private bool UseS3 => _s3 != null && !string.IsNullOrWhiteSpace(_bucket);

        public ImageService(
            IOptions<ImageSettings> settings,
            HttpClient httpClient,
            ILogger<ImageService> logger,
            string contentRootPath,
            IAmazonS3? s3 = null)
        {
            _logger = logger;
            _httpClient = httpClient;
            _s3 = s3;
            _bucket = settings.Value.S3BucketName?.Trim() ?? string.Empty;
            _keyPrefix = NormalizePrefix(settings.Value.S3KeyPrefix);

            var settingsPath = settings.Value.BasePath;
            if (string.IsNullOrWhiteSpace(settingsPath))
            {
                settingsPath = "wwwroot";
            }

            if (Path.IsPathRooted(settingsPath))
            {
                _basePath = settingsPath;
            }
            else
            {
                _basePath = Path.GetFullPath(Path.Combine(contentRootPath, settingsPath));
            }

            if (UseS3)
                _logger.LogInformation("ImageService initialized. Storage: S3 bucket {Bucket}, prefix '{Prefix}'", _bucket, _keyPrefix);
            else
                _logger.LogInformation("ImageService initialized. Storage: local disk, base path {BasePath}", _basePath);
        }

        public async Task<string> SaveImageAsync(Stream fileStream, string fileName, string folderPath)
        {
            try
            {
                var webpFileName = GenerateUniqueFileName(fileName);
                using var image = await Image.LoadAsync(fileStream);
                return await ResizeAndStoreAsync(image, folderPath, webpFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to SaveImageAsync for file: {FileName}", fileName);
                return string.Empty;
            }
        }

        public async Task<string> DownloadAndCompressImageAsync(string imageUrl, string folderPath)
        {
            try
            {
                _logger.LogInformation("Downloading image from URL: {Url}", imageUrl);
                var response = await _httpClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();

                var fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
                if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOf('.') == -1)
                {
                    fileName = "image.jpg";
                }
                var webpFileName = GenerateUniqueFileName(fileName);

                using var stream = await response.Content.ReadAsStreamAsync();
                using var image = await Image.LoadAsync(stream);
                return await ResizeAndStoreAsync(image, folderPath, webpFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download and compress image from URL: {Url}", imageUrl);
                return string.Empty;
            }
        }

        /// <summary>Resizes to max 1200x1200, encodes as WebP and stores it (S3 or local disk).</summary>
        private async Task<string> ResizeAndStoreAsync(Image image, string folderPath, string webpFileName)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(1200, 1200)
            }));

            var encoder = new WebpEncoder { Quality = 75, Method = WebpEncodingMethod.Level4 };
            var relativeResultPath = Path.Combine(folderPath, webpFileName).Replace("\\", "/").TrimStart('/');

            if (UseS3)
            {
                using var ms = new MemoryStream();
                await image.SaveAsync(ms, encoder);
                ms.Position = 0;

                var request = new PutObjectRequest
                {
                    BucketName = _bucket,
                    Key = _keyPrefix + relativeResultPath,
                    InputStream = ms,
                    ContentType = "image/webp",
                    AutoCloseStream = false
                };
                request.Headers.CacheControl = "public, max-age=31536000, immutable";

                _logger.LogInformation("Uploading image to S3: s3://{Bucket}/{Key}", _bucket, request.Key);
                await _s3!.PutObjectAsync(request);
            }
            else
            {
                var localPath = Path.Combine(_basePath, folderPath, webpFileName);
                _logger.LogInformation("Saving image as WebP to local path: {Path}", localPath);
                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                await image.SaveAsync(localPath, encoder);
            }

            _logger.LogInformation("Saved image. Resulting path: {Url}", relativeResultPath);
            return relativeResultPath;
        }

        public async Task<string> CompressImageAsync(string imagePath, int quality = 75)
        {
            // NOTE: operates on a LOCAL file path only (used by local maintenance tasks).
            try
            {
                _logger.LogInformation("Compressing existing image in-place: {Path}", imagePath);
                using var image = await Image.LoadAsync(imagePath);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(1200, 1200)
                }));

                var encoder = new WebpEncoder { Quality = quality, Method = WebpEncodingMethod.Level4 };

                // If it's not webp extension, we save a new webp file and delete old one,
                // or just compress it in-place using WebpEncoder if path allows it.
                if (!imagePath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                {
                    var newPath = Path.ChangeExtension(imagePath, ".webp");
                    await image.SaveAsync(newPath, encoder);
                    if (File.Exists(imagePath)) File.Delete(imagePath);
                    return newPath;
                }

                await image.SaveAsync(imagePath, encoder);
                return imagePath;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to compress image at {Path}, keeping original format/quality.", imagePath);
                return imagePath;
            }
        }

        public void DeleteImage(string imagePath)
        {
            // Sync wrapper kept for the existing interface; the work happens in DeleteImageAsync.
            DeleteImageAsync(imagePath).GetAwaiter().GetResult();
        }

        public async Task DeleteImageAsync(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return;

            // External URLs (https://...) are not ours to delete.
            if (imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;

            var relative = imagePath.Replace("\\", "/").TrimStart('/');
            if (relative.Contains("..")) return; // never allow path tricks

            try
            {
                if (UseS3)
                {
                    await _s3!.DeleteObjectAsync(_bucket, _keyPrefix + relative);
                }
                else
                {
                    var fullPath = Path.Combine(_basePath, relative);
                    if (File.Exists(fullPath)) File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                // Deleting a picture must never break the admin action that triggered it.
                _logger.LogWarning(ex, "Failed to delete image {Path}", imagePath);
            }
        }

        public string GenerateUniqueFileName(string originalFileName) => $"{Guid.NewGuid():N}.webp";

        private static string NormalizePrefix(string? prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return string.Empty;
            var p = prefix.Replace("\\", "/").Trim().Trim('/');
            return p.Length == 0 ? string.Empty : p + "/";
        }
    }
}
