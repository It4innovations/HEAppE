using System.Collections.Generic;
using HEAppE.DomainObjects;

namespace HEAppE.DataAccessTier.IRepository;

public interface IRepository<T> where T : IdentifiableDbEntity
{
    T GetById(long id);
    IList<T> GetAll();
    void Insert(T entity);
    void Delete(long id);
    void Delete(T entityToDelete);
    void Update(T entityToUpdate);
}