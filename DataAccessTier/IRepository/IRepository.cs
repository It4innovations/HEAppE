using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects;

namespace HEAppE.DataAccessTier.IRepository;

public interface IRepository<T> where T : IdentifiableDbEntity
{
    T GetById(long id);
    Task<T> GetByIdAsync(long id);
    IList<T> GetAll();
    Task<IList<T>> GetAllAsync();
    IList<T> GetAllReadOnly();
    Task<IList<T>> GetAllReadOnlyAsync();
    void Insert(T entity);
    void Delete(long id);
    void Delete(T entityToDelete);
    void Update(T entityToUpdate);
    Task DeleteAsync(T entityToDelete);
    Task UpdateAsync(T entityToUpdate);
    void Detach(T entity);
}