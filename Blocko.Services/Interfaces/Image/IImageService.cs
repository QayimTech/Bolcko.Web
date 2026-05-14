namespace Blocko.Services.Interfaces.Image
{
    public interface IImageService
    {
        Task<string> DownloadAndCompressImageAsync(string imageUrl, string folderPath);
        Task<string> SaveImageAsync(IFormFile file, string folderPath);
        Task<string> CompressImageAsync(string imagePath, int quality = 75);
        void DeleteImage(string imagePath);
        string GenerateUniqueFileName(string originalFileName);
    }
}
