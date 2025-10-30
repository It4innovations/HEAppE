using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository;
using HEAppE.DomainObjects;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository;

internal class GenericRepository<T> : IRepository<T> where T : IdentifiableDbEntity
{
    #region Constructors

    protected GenericRepository(MiddlewareContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    #endregion

    #region Instances

    protected readonly MiddlewareContext _context;
    protected readonly DbSet<T> _dbSet;

    #endregion

    #region Methods

    public virtual T GetById(long id)
    {
        return _dbSet.Find(id);
    }

    public virtual IList<T> GetAll()
    {
        return _dbSet.ToList();
    }

    public virtual void Insert(T entity)
    {
        _dbSet.Add(entity);
    }

    public virtual void Delete(long id)
    {
        var entityToDelete = _dbSet.Find(id);
        Delete(entityToDelete);
    }

    public virtual void Delete(T entityToDelete)
    {
        if (_context.Entry(entityToDelete).State == EntityState.Detached) _dbSet.Attach(entityToDelete);
        _dbSet.Remove(entityToDelete);
    }

    public virtual void Update(T entityToUpdate)
    {
        _dbSet.Attach(entityToUpdate);
        _context.Entry(entityToUpdate).State = EntityState.Modified;
    }
    
    public virtual void Detach(T entity)
    {
        _context.Entry(entity).State = EntityState.Detached;
    }


    #endregion
}