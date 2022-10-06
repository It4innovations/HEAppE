using System.Collections.Generic;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.Notifications;
using HEAppE.DomainObjects.OpenStack;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier
{
    public class MiddlewareContextSettings
    {
        public static string ConnectionString { get; set; }

        public static List<AdaptorUser> AdaptorUsers { get; set; }

        public static List<AdaptorUserRole> AdaptorUserRoles { get; set; }

        public static List<AdaptorUserGroup> AdaptorUserGroups { get; set; }

        public static List<AdaptorUserUserGroup> AdaptorUserUserGroups { get; set; }

        public static List<AdaptorUserUserRole> AdaptorUserUserRoles { get; set; }


        public static List<OpenStackInstance> OpenStackInstances { get; set; }

        public static List<OpenStackDomain> OpenStackDomains { get; set; }

        public static List<OpenStackProject> OpenStackProjects { get; set; }

        public static List<OpenStackProjectDomain> OpenStackProjectDomains { get; set; }

        public static List<OpenStackAuthenticationCredential> OpenStackAuthenticationCredentials { get; set; }

        public static List<OpenStackAuthenticationCredentialDomain> OpenStackAuthenticationCredentialDomains { get; set; }

        public static List<OpenStackAuthenticationCredentialProjectDomain> OpenStackAuthenticationCredentialProjectDomains { get; set; }


        public static List<ClusterProxyConnection> ClusterProxyConnections { get; set; }

        public static List<Cluster> Clusters { get; set; }

        public static List<ClusterAuthenticationCredentials> ClusterAuthenticationCredentials { get; set; }

        public static List<ClusterNodeType> ClusterNodeTypes { get; set; }

        public static List<CommandTemplate> CommandTemplates { get; set; }

        public static List<CommandTemplateParameter> CommandTemplateParameters { get; set; }

        public static List<FileTransferMethod> FileTransferMethods { get; set; }
        public static List<Project> Projects { get; set; }
        public static List<ClusterProject> ClusterProjects { get; set; }
        public static List<ClusterProjectCredentials> ClusterProjectCredentials { get; set; }
        public static List<Language> Languages { get; set; }
    }
}
