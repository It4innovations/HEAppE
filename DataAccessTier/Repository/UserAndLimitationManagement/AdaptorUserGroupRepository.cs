using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement
{
    internal class AdaptorUserGroupRepository : GenericRepository<AdaptorUserGroup>, IAdaptorUserGroupRepository
    {
        #region Instances
        private readonly string _defaultGroupName = "default";
        #endregion
        #region Constructors
        internal AdaptorUserGroupRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
        #region Methods
        public IEnumerable<AdaptorUserGroup> GetAllWithAdaptorUserGroups()
        {
            return _dbSet.Include(i => i.AdaptorUserUserGroups).ToList();
        }

        public AdaptorUserGroup GetDefaultSubmitterGroup()
        {
            return GetAll().FirstOrDefault(w => w.Name == _defaultGroupName);
        }

        public AdaptorUserGroup GetGroupByUniqueName(string groupName)
        {
            return GetAll().SingleOrDefault(g => g.Name == groupName);
        }
        #endregion
    }
}