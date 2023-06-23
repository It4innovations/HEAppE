using HEAppE.DataAccessTier.IRepository.OpenStack;
using HEAppE.DomainObjects.OpenStack;
using System.Linq;

namespace HEAppE.DataAccessTier.Repository.OpenStack
{
    internal class OpenStackProjectRepository : GenericRepository<OpenStackProject>, IOpenStackProjectRepository
    {
        #region Constructors
        internal OpenStackProjectRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
        #region Methods
        public OpenStackProject GetOpenStackProjectByProjectId(long projectId)
        {
            return _dbSet.FirstOrDefault(f => f.AdaptorUserGroup.ProjectId == projectId);
        }
        #endregion
    }
}
