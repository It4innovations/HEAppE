using System;
using System.Linq;
using System.Collections.Generic;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement
{
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
            DateTime currentTime = DateTime.UtcNow;
            return GetAll().Where(s => s.ExpirationTime < currentTime)
                         .ToList();
        }
        #endregion
    }
}