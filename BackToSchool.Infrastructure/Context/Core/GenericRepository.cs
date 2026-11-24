using BackToSchool.core.Interfaces;
using BackToSchool.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace BackToSchool.Infrastructure.Context.Core;

public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _dbSet = context.Set<TEntity>();
    }

    public IQueryable<TEntity> Query() => _dbSet.AsNoTracking();

    public IQueryable<TEntity> Tracked() => _dbSet;

    public async Task<TEntity?> GetByIdAsync(long id) => await _dbSet.FindAsync(id);

    public async Task AddAsync(TEntity entity) => await _dbSet.AddAsync(entity);

    public void Update(TEntity entity) => _dbSet.Update(entity);

    public void Remove(TEntity entity) => _dbSet.Remove(entity);
}