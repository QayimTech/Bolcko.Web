using Blocko.Services.Interfaces.Image;

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

        // ??????? ?????????: ??????? IOptions ?????? ?????????
        public ImageService(IOptions<ImageSettings> settings, HttpClient httpClient)
        {
            _basePath = settings.Value.BasePath;
            _httpClient = httpClient;
        }

        public async Task<string> SaveImageAsync(Stream fileStream, string fileName, string folderPath)
        {
            try
            {
                var uniqueFileName = GenerateUniqueFileName(fileName);
                var localPath = Path.Combine(_basePath, folderPath, uniqueFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                using (var destinationStream = new FileStream(localPath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(destinationStream);
                }

                await CompressImageAsync(localPath);

                return Path.Combine(folderPath, uniqueFileName).Replace("\\", "/");
            }
            catch { return string.Empty; }
        }

        public async Task<string> DownloadAndCompressImageAsync(string imageUrl, string folderPath)
        {
            try
            {
                var response = await _httpClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();

                var fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
                var uniqueFileName = GenerateUniqueFileName(fileName);
                var localPath = Path.Combine(_basePath, folderPath, uniqueFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(localPath))
                {
                    await stream.CopyToAsync(fileStream);
                }

                await CompressImageAsync(localPath);

                return Path.Combine(folderPath, uniqueFileName).Replace("\\", "/");
            }
            catch { return string.Empty; }
        }

        public async Task<string> CompressImageAsync(string imagePath, int quality = 75)
        {
            try
            {
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
            catch { return imagePath; }
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