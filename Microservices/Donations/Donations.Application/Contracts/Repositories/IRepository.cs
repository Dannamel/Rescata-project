namespace Donations.Application.Contracts.Repositories;

/// <summary>
/// Repositorio genérico. Los cambios se confirman con <see cref="Persistence.IUnitOfWork.CommitAsync"/>.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);

    Task<T> CreateAsync(T entity);

    Task UpdateAsync(T entity);
}
