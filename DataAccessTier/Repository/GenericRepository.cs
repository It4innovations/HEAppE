#pragma warning disable CS1998
﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    
    public virtual async Task<T> GetByIdAsync(long id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual IList<T> GetAll()
    {
        return _dbSet.ToList();
    }
    
    public virtual async Task<IList<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
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
        if (entityToDelete == null) return;

        var trackedEntry = _context.ChangeTracker.Entries<T>()
            .FirstOrDefault(e => e.Entity.Id == entityToDelete.Id);
        
        if (trackedEntry != null)
        {
            if (!ReferenceEquals(trackedEntry.Entity, entityToDelete))
            {
                trackedEntry.State = EntityState.Detached;
            }
        }

        if (_context.Entry(entityToDelete).State == EntityState.Detached) _dbSet.Attach(entityToDelete);
        _dbSet.Remove(entityToDelete);
    }

    public virtual void Update(T entityToUpdate)
    {
        if (entityToUpdate == null) return;

        var trackedEntry = _context.ChangeTracker.Entries<T>()
            .FirstOrDefault(e => e.Entity.Id == entityToUpdate.Id);
        
        if (trackedEntry != null)
        {
            if (ReferenceEquals(trackedEntry.Entity, entityToUpdate))
            {
                trackedEntry.State = EntityState.Modified;
                return;
            }
            else
            {
                trackedEntry.State = EntityState.Detached;
            }
        }

        _dbSet.Attach(entityToUpdate);
        _context.Entry(entityToUpdate).State = EntityState.Modified;
    }

    public async Task DeleteAsync(T entityToDelete)
    {
        await Task.Run(() => Delete(entityToDelete));
    }
    
    public async Task DeleteAsync(long id)
    {
        await Task.Run(() => Delete(id));
    }

    public async Task UpdateAsync(T entityToUpdate)
    {
        if (entityToUpdate == null) return;

        var trackedEntry = _context.ChangeTracker.Entries<T>()
            .FirstOrDefault(e => e.Entity.Id == entityToUpdate.Id);
        
        if (trackedEntry != null)
        {
            if (ReferenceEquals(trackedEntry.Entity, entityToUpdate))
            {
                trackedEntry.State = EntityState.Modified;
                return;
            }
            else
            {
                trackedEntry.State = EntityState.Detached;
            }
        }

        _dbSet.Attach(entityToUpdate);
        _context.Entry(entityToUpdate).State = EntityState.Modified;
    }

    public virtual void Detach(T entity)
    {
        _context.Entry(entity).State = EntityState.Detached;
    }


    #endregion
}