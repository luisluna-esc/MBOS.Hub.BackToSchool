namespace BackToSchool.core.Interfaces;

public interface IGenericRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> Query();

    /// <summary>Igual que Query(), pero EF sigue los cambios: usar cuando se van a modificar las entidades leídas.</summary>
    IQueryable<TEntity> Tracked();
    Task<TEntity?> GetByIdAsync(long id);
    Task AddAsync(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}