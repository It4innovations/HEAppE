using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.ClusterInformation;
using System.Collections.Generic;
using System.Linq;

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

        #region Methods
        public IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsForClusterAndProject(long clusterId, long projectId)
        {
            var clusterProject = _context.ClusterProjects.FirstOrDefault(cp => cp.ClusterId == clusterId && cp.ProjectId == projectId);
            var clusterProjectCredentials = clusterProject?.ClusterProjectCredentials.FindAll(cpc => !cpc.IsServiceAccount);
            var credentials = clusterProjectCredentials?.Select(c => c.ClusterAuthenticationCredentials).Where(x => !x.IsDeleted);
            return credentials?.ToList() ?? new List<ClusterAuthenticationCredentials>();
        }

        public ClusterAuthenticationCredentials GetServiceAccountCredentials(long clusterId, long projectId)
        {
            var clusterProject = _context.ClusterProjects.FirstOrDefault(cp => cp.ClusterId == clusterId && cp.ProjectId == projectId);
            var clusterProjectCredentials = clusterProject?.ClusterProjectCredentials.FindAll(cpc => cpc.IsServiceAccount);
            var credentials = clusterProjectCredentials?.Select(c => c.ClusterAuthenticationCredentials).Where(x => !x.IsDeleted);
            return credentials?.FirstOrDefault();
        }

        public IEnumerable<ClusterAuthenticationCredentials> GetAllGeneratedWithFingerprint(string fingerprint, long projectId)
        {
            var credentials = _context.ClusterAuthenticationCredentials.Where(x => x.IsGenerated && !x.IsDeleted && x.PublicKeyFingerprint == fingerprint && x.ClusterProjectCredentials.Any(y => y.ClusterProject.ProjectId == projectId));
            return credentials?.ToList() ?? new List<ClusterAuthenticationCredentials>();

        }
        #endregion
    }
}
