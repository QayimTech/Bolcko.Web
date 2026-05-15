using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bolcko.Domain.Common
{
    public interface IPagedList<T>
    {
        IEnumerable<T> Items { get; }
        int TotalCount { get; }
        int PageIndex { get; }
        int PageSize { get; }
        int TotalPages { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
    }

}
