using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;

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
        return GetAll().Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.AuthenticationTime)
            .FirstOrDefault();
    }

    public IList<OpenStackSession> GetAllActive()
    {
        var currentTime = DateTime.UtcNow;
        return GetAll().Where(s => s.ExpirationTime < currentTime)
            .ToList();
    }

    #endregion
}