﻿using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
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

        #region Methods
        public IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsForClusterAndProject(long clusterId, long projectId)
        {
            var clusterProject = _context.ClusterProjects.FirstOrDefault(cp => cp.ClusterId == clusterId && cp.ProjectId == projectId);
            var clusterProjectCredentials = clusterProject?.ClusterProjectCredentials.FindAll(cpc => !cpc.IsServiceAccount);
            var credentials = clusterProjectCredentials?.Select(c => c.ClusterAuthenticationCredentials);
            return credentials?.ToList() ?? new List<ClusterAuthenticationCredentials>();
        }

        public ClusterAuthenticationCredentials GetServiceAccountCredentials(long clusterId, long projectId)
        {
            var clusterProject = _context.ClusterProjects.FirstOrDefault(cp => cp.ClusterId == clusterId && cp.ProjectId == projectId);
            var clusterProjectCredentials = clusterProject?.ClusterProjectCredentials.FindAll(cpc => cpc.IsServiceAccount);
            var credentials = clusterProjectCredentials?.Select(c => c.ClusterAuthenticationCredentials);
            return credentials?.FirstOrDefault();
        }

        public IEnumerable<ClusterAuthenticationCredentials> GetAllGeneratedWithFingerprint(string fingerprint)
        {
            var credentials = _context.ClusterAuthenticationCredentials.Where(x => x.IsGenerated && x.PublicKeyFingerprint == fingerprint);
            return credentials?.ToList() ?? new List<ClusterAuthenticationCredentials>();
        }
        #endregion
    }
}
