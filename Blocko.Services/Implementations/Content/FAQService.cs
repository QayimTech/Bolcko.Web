using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blocko.Services.Interfaces.Content;
using Bolcko.Domain.Entities.Content;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.Content
{
    public class FAQService : IFAQService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FAQService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<FAQItem>> GetAllFAQsAsync()
        {
            var items = await _unitOfWork.FAQs.GetAllAsync();
            return items.OrderBy(x => x.PageTarget).ThenBy(x => x.DisplayOrder).ToList();
        }

        public async Task<IEnumerable<FAQItem>> GetActiveFAQsByPageAsync(string pageTarget)
        {
            var items = await _unitOfWork.FAQs.FindAsync(x => x.IsActive && x.PageTarget.ToLower() == pageTarget.ToLower());
            return items.OrderBy(x => x.DisplayOrder).ToList();
        }

        public async Task<FAQItem?> GetFAQByIdAsync(int id)
        {
            return await _unitOfWork.FAQs.GetByIdAsync(id);
        }

        public async Task CreateFAQAsync(FAQItem item)
        {
            item.CreatedAt = DateTime.UtcNow;
            await _unitOfWork.FAQs.AddAsync(item);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateFAQAsync(FAQItem item)
        {
            item.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.FAQs.Update(item);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteFAQAsync(int id)
        {
            var item = await _unitOfWork.FAQs.GetByIdAsync(id);
            if (item != null)
            {
                _unitOfWork.FAQs.Remove(item);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task ToggleActiveAsync(int id)
        {
            var item = await _unitOfWork.FAQs.GetByIdAsync(id);
            if (item != null)
            {
                item.IsActive = !item.IsActive;
                item.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.FAQs.Update(item);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}