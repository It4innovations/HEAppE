using System.Linq;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;

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
        public AdaptorUserGroup GetDefaultSubmitterGroup()
        {
            return GetAll().Where(w => w.Name == _defaultGroupName)
                         .FirstOrDefault();
        }

        public AdaptorUserGroup GetGroupByUniqueName(string groupName)
        {
            return GetAll().Single(g => g.Name == groupName);
        }
        #endregion
    }
}