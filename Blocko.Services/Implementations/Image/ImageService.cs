using Blocko.Services.Interfaces.Image;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blocko.Services.Implementations.Images
{
    public class ImageService : IImageService
    {
        private readonly string _basePath;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ImageService> _logger;

        public ImageService(IOptions<ImageSettings> settings, HttpClient httpClient, ILogger<ImageService> logger, string contentRootPath)
        {
            _logger = logger;
            _httpClient = httpClient;

            // Resolve the absolute path relative to ContentRootPath
            // to make sure it always points to the correct wwwroot folder
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

            _logger.LogInformation("ImageService initialized. Base image path: {BasePath}", _basePath);
        }

        public async Task<string> SaveImageAsync(Stream fileStream, string fileName, string folderPath)
        {
            try
            {
                var uniqueFileName = GenerateUniqueFileName(fileName);
                var localPath = Path.Combine(_basePath, folderPath, uniqueFileName);

                _logger.LogInformation("Saving product image to local path: {Path}", localPath);

                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                using (var destinationStream = new FileStream(localPath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(destinationStream);
                }

                await CompressImageAsync(localPath);

                var relativeResultPath = Path.Combine(folderPath, uniqueFileName).Replace("\\", "/");
                _logger.LogInformation("Successfully saved and compressed image. Resulting URL path: {Url}", relativeResultPath);
                return relativeResultPath;
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
                var uniqueFileName = GenerateUniqueFileName(fileName);
                var localPath = Path.Combine(_basePath, folderPath, uniqueFileName);

                _logger.LogInformation("Saving downloaded image to path: {Path}", localPath);

                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(localPath))
                {
                    await stream.CopyToAsync(fileStream);
                }

                await CompressImageAsync(localPath);

                var relativeResultPath = Path.Combine(folderPath, uniqueFileName).Replace("\\", "/");
                return relativeResultPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download and compress image from URL: {Url}", imageUrl);
                return string.Empty;
            }
        }

        public async Task<string> CompressImageAsync(string imagePath, int quality = 75)
        {
            try
            {
                _logger.LogInformation("Compressing image: {Path}", imagePath);
                using var image = await Image.LoadAsync(imagePath);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(1200, 1200)
                }));

                var encoder = new JpegEncoder { Quality = quality };
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
            if (string.IsNullOrWhiteSpace(imagePath)) return;
            var fullPath = Path.Combine(_basePath, imagePath.TrimStart('/'));
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }

        public string GenerateUniqueFileName(string originalFileName) => $"{Guid.NewGuid():N}.jpg";
    }
}