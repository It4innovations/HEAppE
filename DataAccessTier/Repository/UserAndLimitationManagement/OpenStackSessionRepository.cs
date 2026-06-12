using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement;

internal class OpenStackSessionRepository : GenericRepository<OpenStackSession>, IOpenStackSessionRepository
{
    #region Constructors

    internal OpenStackSessionRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public OpenStackSession GetByUser(AdaptorUser user)
    {
        return _dbSet.Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.AuthenticationTime)
            .FirstOrDefault();
    }

    public async Task<OpenStackSession> GetByUserAsync(AdaptorUser user)
    {
        return await _dbSet.Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.AuthenticationTime)
            .FirstOrDefaultAsync();
    }

    public IList<OpenStackSession> GetAllActive()
    {
        var currentTime = DateTime.UtcNow;
        return _dbSet.Where(s => s.ExpirationTime < currentTime)
            .ToList();
    }

    public async Task<IList<OpenStackSession>> GetAllActiveAsync()
    {
        var currentTime = DateTime.UtcNow;
        return await _dbSet.Where(s => s.ExpirationTime < currentTime)
            .ToListAsync();
    }

    #endregion
}