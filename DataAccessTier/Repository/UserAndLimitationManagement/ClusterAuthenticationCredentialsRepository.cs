using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.ClusterInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement
{
    internal class ClusterAuthenticationCredentialsRepository : GenericRepository<ClusterAuthenticationCredentials>, IClusterAuthenticationCredentialsRepository
    {
        #region Constructors
        internal ClusterAuthenticationCredentialsRepository(MiddlewareContext context)
                : base(context)
        {

        }
        #endregion
    }
}
