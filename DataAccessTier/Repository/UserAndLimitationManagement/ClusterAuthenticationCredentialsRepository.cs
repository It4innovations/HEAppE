using System.Collections.Generic;
using System.Linq;

using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DataAccessTier.Vault;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement
{
    internal class ClusterAuthenticationCredentialsRepository : GenericRepository<ClusterAuthenticationCredentials>, IClusterAuthenticationCredentialsRepository
    {
        private readonly IVaultConnector _vaultConnector;
        #region Constructors
        internal ClusterAuthenticationCredentialsRepository(MiddlewareContext context, IVaultConnector vaultConnector)
                : base(context)
        {
            _vaultConnector = vaultConnector;
        }
        #endregion

        #region Methods

        public override ClusterAuthenticationCredentials GetById(long id)
        {
            var dbEntity = base.GetById(id);
            var vaultData = _vaultConnector.GetClusterAuthenticationCredentials(id);
            dbEntity.ImportVaultData(vaultData);
            return dbEntity;
        }

        public override void Delete(ClusterAuthenticationCredentials entityToDelete)
        {
            _vaultConnector.DeleteClusterAuthenticationCredentials(entityToDelete.Id);
            base.Delete(entityToDelete);
        }

        public override void Delete(long id)
        {
            _vaultConnector.DeleteClusterAuthenticationCredentials(id);
            base.Delete(id);
        }

        public override void Insert(ClusterAuthenticationCredentials entity)
        {
            _vaultConnector.SetClusterAuthenticationCredentials(entity.ExportVaultData());
            base.Insert(entity);
        }

        public override void Update(ClusterAuthenticationCredentials entityToUpdate)
        {
            base.Update(entityToUpdate);
            _vaultConnector.SetClusterAuthenticationCredentials(entityToUpdate.ExportVaultData());
        }

        public override IList<ClusterAuthenticationCredentials> GetAll()
        {
            return WithVaultData(base.GetAll()).ToList();
        }

        public IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsForClusterAndProject(long clusterId, long projectId)
        {
            var clusterProject = _context.ClusterProjects.FirstOrDefault(cp => cp.ClusterId == clusterId && cp.ProjectId == projectId);
            var clusterProjectCredentials = clusterProject?.ClusterProjectCredentials.FindAll(cpc => !cpc.IsServiceAccount);
            var credentials = clusterProjectCredentials?.Where(x => !x.IsDeleted)?.Select(c => c.ClusterAuthenticationCredentials).ToList();
            return WithVaultData(credentials);
        }

        public IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsForUsernameAndProject(string username, long projectId)
        {
            var clusterAuthenticationCredentials = _context.ClusterAuthenticationCredentials.Where(cpc => cpc.Username == username && cpc.ClusterProjectCredentials.Any(c => c.ClusterProject.ProjectId == projectId));
            var credentials = clusterAuthenticationCredentials?.Where(x => !x.IsDeleted)?.Select(x => x).ToList();
            return WithVaultData(credentials).ToList();
        }

        public IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsProject(long projectId)
        {
            var clusterAuthenticationCredentials = _context.ClusterAuthenticationCredentials.Where(cpc => cpc.ClusterProjectCredentials.Any(c => c.ClusterProject.ProjectId == projectId));
            var credentials = clusterAuthenticationCredentials?.Where(x => !x.IsDeleted)?.Select(x => x).ToList();

            return WithVaultData(credentials);
        }

        public ClusterAuthenticationCredentials GetServiceAccountCredentials(long clusterId, long projectId)
        {
            var clusterProject = _context.ClusterProjects.FirstOrDefault(cp => cp.ClusterId == clusterId && cp.ProjectId == projectId);
            var clusterProjectCredentials = clusterProject?.ClusterProjectCredentials.FindAll(cpc => cpc.IsServiceAccount);
            var credentials = clusterProjectCredentials?.Where(x => !x.IsDeleted)?.Select(c => c.ClusterAuthenticationCredentials);
            var cred = credentials?.FirstOrDefault();
            return WithVaultData(cred);
        }

        public IEnumerable<ClusterAuthenticationCredentials> GetAllGeneratedWithFingerprint(string fingerprint, long projectId)
        {
            var credentials = _context.ClusterAuthenticationCredentials
                .Where(x => x.IsGenerated && !x.IsDeleted && x.PublicKeyFingerprint == fingerprint && x.ClusterProjectCredentials.Any(y => y.ClusterProject.ProjectId == projectId))
                .ToList();
            return WithVaultData(credentials);

        }

        public IEnumerable<ClusterAuthenticationCredentials> GetAllGenerated(long projectId)
        {
            var credentials = _context.ClusterAuthenticationCredentials
                .Where(x => x.IsGenerated && !x.IsDeleted && x.ClusterProjectCredentials.Any(y => y.ClusterProject.ProjectId == projectId))
                .ToList();
            return WithVaultData(credentials);

        }
        #endregion


        private IEnumerable<ClusterAuthenticationCredentials> WithVaultData(IEnumerable<ClusterAuthenticationCredentials> credentials)
        {
            if (credentials == null)
            {
                return Enumerable.Empty<ClusterAuthenticationCredentials>();
            }
            foreach (var item in credentials)
            {
                var vaultData = _vaultConnector.GetClusterAuthenticationCredentials(item.Id);
                item.ImportVaultData(vaultData);
            }
            return credentials;
        }

        private ClusterAuthenticationCredentials WithVaultData(ClusterAuthenticationCredentials credentials)
        {
            if (credentials != null)
            {
                var vaultData = _vaultConnector.GetClusterAuthenticationCredentials(credentials.Id);
                credentials.ImportVaultData(vaultData);
            }

            return credentials;
        }
    }
}
