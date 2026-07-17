using System.Linq.Expressions;
using Bolcko.Domain.Common;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly BlockoDbContext _context;

        public GenericRepository(BlockoDbContext context)
        {
            _context = context;
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }

        public IQueryable<T> GetAllAsQueryable()
        {
            return _context.Set<T>().AsQueryable();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().Where(predicate).ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
        }

        public void Update(T entity)
        {
            _context.Set<T>().Update(entity);
        }

        public void Remove(T entity)
        {
            _context.Set<T>().Remove(entity);
        }

        public async Task<IPagedList<T>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, params Expression<Func<T, object>>[] includes)
        {
            // 1. Start with base query using AsNoTracking to optimize performance
            IQueryable<T> baseQuery = _context.Set<T>().AsNoTracking();

            if (predicate != null)
            {
                baseQuery = baseQuery.Where(predicate);
            }

            // 2. Calculate the count instantly without heavy Includes or sorting overhead
            var totalCount = await baseQuery.CountAsync();

            // 3. Build data query and apply includes with AsSplitQuery to avoid Cartesian Explosion
            IQueryable<T> dataQuery = baseQuery;

            foreach (var include in includes)
            {
                dataQuery = dataQuery.Include(include);
            }

            // Apply split query layout dynamically to prevent connection pooling and memory exhaustion on heavy datasets
            dataQuery = dataQuery.AsSplitQuery();

            if (orderBy != null)
            {
                dataQuery = orderBy(dataQuery);
            }

            var items = await dataQuery
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new Common.PagedList<T>(items, totalCount, pageIndex, pageSize);
        }
    }
}