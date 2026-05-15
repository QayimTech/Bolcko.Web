using Blocko.Persistence.Common;
using Bolcko.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Extensions
{
    public static class QueryableExtensions
    {
        public static async Task<IPagedList<T>> ToPagedListAsync<T>(this IQueryable<T> source, int pageIndex, int pageSize)
        {
            var totalCount = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedList<T>(items, totalCount, pageIndex, pageSize);
        }
    }
}
