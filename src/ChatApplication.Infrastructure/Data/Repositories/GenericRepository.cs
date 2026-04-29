using ChatApplication.Core.Common.Base;
using ChatApplication.Core.Common.Interfaces;
using ChatApplication.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.Infrastructure.Data.Repositories;

public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(string id) => await _dbSet.FindAsync(id);
    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();
    public async Task<T> AddAsync(T entity) { await _dbSet.AddAsync(entity); return entity; }
    public Task UpdateAsync(T entity) { _dbSet.Update(entity); return Task.CompletedTask; }
    public async Task DeleteAsync(string id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null) _dbSet.Remove(entity);
    }
}
