using Blocko.Services.Interfaces.Image;
using System.Drawing;
using System.Drawing.Imaging;

namespace Blocko.Services.Implementations.Image
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly HttpClient _httpClient;

        public ImageService(IWebHostEnvironment environment, HttpClient httpClient)
        {
            _environment = environment;
            _httpClient = httpClient;
        }

        public async Task<string> DownloadAndCompressImageAsync(string imageUrl, string folderPath)
        {
            try
            {
                var response = await _httpClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();

                var fileName = GenerateUniqueFileName(Path.GetFileName(imageUrl) ?? "image.jpg");
                var localPath = Path.Combine(_environment.WebRootPath, folderPath, fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(localPath))
                {
                    await stream.CopyToAsync(fileStream);
                }

                await CompressImageAsync(localPath);

                return $"/{folderPath}/{fileName}";
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<string> SaveImageAsync(IFormFile file, string folderPath)
        {
            var fileName = GenerateUniqueFileName(file.FileName);
            var localPath = Path.Combine(_environment.WebRootPath, folderPath, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

            using (var stream = new FileStream(localPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            await CompressImageAsync(localPath);

            return $"/{folderPath}/{fileName}";
        }

        public async Task<string> CompressImageAsync(string imagePath, int quality = 75)
        {
            try
            {
                using (var image = Image.FromFile(imagePath))
                {
                    var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                    var compressedPath = imagePath;
                    using (var stream = new FileStream(compressedPath, FileMode.Create))
                    {
                        image.Save(stream, jpegEncoder, encoderParams);
                    }
                }

                return imagePath;
            }
            catch
            {
                return imagePath;
            }
        }

        public void DeleteImage(string imagePath)
        {
            if (!string.IsNullOrEmpty(imagePath))
            {
                var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }

        public string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileName = Path.GetFileNameWithoutExtension(originalFileName);
            return $"{fileName}_{Guid.NewGuid():N}{extension}";
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
