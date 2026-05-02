using Bolcko.Domain.Entities.SEO;
using Bolcko.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Blocko.Persistence.Repositories.SEO
{
    public class SEORepositroy : GenericRepository<SEOMetadata>, ISEORepository
    {
        public SEORepositroy(BlockoDbContext context):base(context)
        {
            
        }

        public async Task<SEOMetadata?> GetByPageNameAsync(string pageName)
        {
          //return await _context.SEOMetadata.FirstOrDefaultAsync(s => s.PageName == pageName);
            return await Task.FromResult(_context.SEOMetadata.FirstOrDefault(s => s.PageName == pageName));
        }
    }
}
