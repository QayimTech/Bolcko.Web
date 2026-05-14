using System.IO;
using System.Threading.Tasks;

namespace Blocko.Services.Interfaces.Image
{
    public interface IImageService
    {
        Task<string> SaveImageAsync(Stream fileStream, string fileName, string folderPath);
        Task<string> DownloadAndCompressImageAsync(string imageUrl, string folderPath);
        Task<string> CompressImageAsync(string imagePath, int quality = 75);
        void DeleteImage(string imagePath);
        string GenerateUniqueFileName(string originalFileName);
    }
}
