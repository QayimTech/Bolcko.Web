using System.Collections.Generic;
using System.Threading.Tasks;
using Bolcko.Domain.Entities.Content;

namespace Blocko.Services.Interfaces.Content
{
    public interface IFAQService
    {
        Task<IEnumerable<FAQItem>> GetAllFAQsAsync();
        Task<IEnumerable<FAQItem>> GetActiveFAQsByPageAsync(string pageTarget);
        Task<FAQItem?> GetFAQByIdAsync(int id);
        Task CreateFAQAsync(FAQItem item);
        Task UpdateFAQAsync(FAQItem item);
        Task DeleteFAQAsync(int id);
        Task ToggleActiveAsync(int id);
    }
}