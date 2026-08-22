using System.Threading.Tasks;

namespace Blocko.Services.Interfaces
{
    public interface ITranslationService
    {
        Task<string> TranslateAsync(string text, string targetLanguage);
    }
}
