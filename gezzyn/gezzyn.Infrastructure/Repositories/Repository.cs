using System.Linq.Expressions;
using gezzyn.Domain.Interfaces;
using gezzyn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace gezzyn.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly ApplicationDbContext _context;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<T?> GetByIdAsync(object id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task<List<T>> GetAllAsListAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _context.Set<T>().Where(predicate).ToListAsync();
    }

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _context.Set<T>().FirstOrDefaultAsync(predicate);
    }

    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
    }

    public async Task AddRangeAsync(List<T> entities)
    {
        await _context.Set<T>().AddRangeAsync(entities);
    }

    public Task UpdateAsync(T entity)
    {
        _context.Set<T>().Update(entity);
        return Task.CompletedTask;
    }

    public Task UpdateRangeAsync(List<T> entities)
    {
        _context.Set<T>().UpdateRange(entities);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(object id)
    {
        var entity = _context.Set<T>().Find(id);
        if (entity != null)
        {
            _context.Set<T>().Remove(entity);
        }

        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(List<object> ids)
    {
        foreach (var item in ids)
        {
            DeleteAsync(item);
        }

        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(object id)
    {
        var entity = await _context.Set<T>().FindAsync(id);
        return entity != null;
    }

    public IQueryable<T> AsQueryable()
    {
        return _context.Set<T>().AsQueryable();
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await _context.Set<T>().AnyAsync(predicate);
    }
}
