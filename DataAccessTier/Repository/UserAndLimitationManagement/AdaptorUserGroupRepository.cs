using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

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
        public AdaptorUserGroup GetByIdWithAdaptorUserGroups(long id)
        {
            return _dbSet.Where(w => w.Id == id)
                          .Include(i => i.AdaptorUserUserGroupRoles)
                          .ThenInclude(i => i.AdaptorUser)
                          .FirstOrDefault();
        }

        public IEnumerable<AdaptorUserGroup> GetAllWithAdaptorUserGroupsAndProject()
        {
            return _dbSet.Include(p => p.Project)
                            .ThenInclude(i => i.CommandTemplates)
                            .ThenInclude(i => i.TemplateParameters)
                            .Include(i => i.AdaptorUserUserGroupRoles)
                            .ThenInclude(i => i.AdaptorUser)
                            .ToList();
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