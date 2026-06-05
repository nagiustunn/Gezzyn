using System.Linq.Expressions;

namespace gezzyn.Domain.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(object id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<List<T>> GetAllAsListAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(object id);
        Task<bool> ExistsAsync(object id);
        IQueryable<T> AsQueryable();
        Task AddRangeAsync(List<T> entities);
        Task UpdateRangeAsync(List<T> entities);
        Task DeleteRangeAsync(List<object> ids);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    }
}
