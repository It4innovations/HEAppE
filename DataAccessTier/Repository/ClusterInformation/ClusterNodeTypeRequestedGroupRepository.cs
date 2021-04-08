using HEAppE.DataAccessTier.IRepository.ClusterInformation;
using HEAppE.DomainObjects.ClusterInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.Repository.ClusterInformation
{
    internal class ClusterNodeTypeRequestedGroupRepository : GenericRepository<ClusterNodeTypeRequestedGroup>, IClusterNodeTypeRequestedGroupRepository
    {
        #region Constructors
        internal ClusterNodeTypeRequestedGroupRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}
