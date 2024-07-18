using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.OpenStack;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DataAccessTier
{
    public class MiddlewareContextSettings
    {
        public static string ConnectionString { get; set; }
        public static List<AdaptorUser> AdaptorUsers { get; set; } = new List<AdaptorUser>();

        public static List<AdaptorUserRole> AdaptorUserRoles { get; set; } = new List<AdaptorUserRole>();

        public static List<AdaptorUserGroup> AdaptorUserGroups { get; set; } = new List<AdaptorUserGroup>();

        public static List<AdaptorUserUserGroupRole> AdaptorUserUserGroupRoles { get; set; } = new List<AdaptorUserUserGroupRole>();

        public static List<OpenStackInstance> OpenStackInstances { get; set; } = new List<OpenStackInstance>();

        public static List<OpenStackDomain> OpenStackDomains { get; set; } = new List<OpenStackDomain>();

        public static List<OpenStackProjectDomain> OpenStackProjectDomains { get; set; } = new List<OpenStackProjectDomain>();

        public static List<OpenStackProject> OpenStackProjects { get; set; } = new List<OpenStackProject>();

        public static List<OpenStackAuthenticationCredential> OpenStackAuthenticationCredentials { get; set; } = new List<OpenStackAuthenticationCredential>();

        public static List<OpenStackAuthenticationCredentialDomain> OpenStackAuthenticationCredentialDomains { get; set; } = new List<OpenStackAuthenticationCredentialDomain>();

        public static List<OpenStackAuthenticationCredentialProject> OpenStackAuthenticationCredentialProjects { get; set; } = new List<OpenStackAuthenticationCredentialProject>();

        public static List<ClusterProxyConnection> ClusterProxyConnections { get; set; } = new List<ClusterProxyConnection>();

        public static List<Cluster> Clusters { get; set; } = new List<Cluster>();

        public static List<ClusterAuthenticationCredentials> ClusterAuthenticationCredentials { get; set; } = new List<ClusterAuthenticationCredentials>();

        public static List<ClusterNodeType> ClusterNodeTypes { get; set; } = new List<ClusterNodeType>();

        public static List<CommandTemplate> CommandTemplates { get; set; } = new List<CommandTemplate>();

        public static List<CommandTemplateParameter> CommandTemplateParameters { get; set; } = new List<CommandTemplateParameter>();

        public static List<FileTransferMethod> FileTransferMethods { get; set; } = new List<FileTransferMethod>();

        public static List<Contact> Contacts { get; set; } = new List<Contact>();

        public static List<Project> Projects { get; set; } = new List<Project>();
        public static List<SubProject> SubProjects { get; set; } = new List<SubProject>();
        public static List<Accounting> Accountings { get; set; } = new List<Accounting>();
        public static List<AccountingState> AccountingStates { get; set; } = new List<AccountingState>();
        public static List<ClusterNodeTypeAggregation> ClusterNodeTypeAggregations { get; set; } = new List<ClusterNodeTypeAggregation>();
        public static List<ClusterNodeTypeAggregationAccounting> ClusterNodeTypeAggregationAccounting { get; set; } = new List<ClusterNodeTypeAggregationAccounting>();
        public static List<ProjectClusterNodeTypeAggregation> ProjectClusterNodeTypeAggregations { get; set; } = new List<ProjectClusterNodeTypeAggregation>();
        public static List<ProjectContact> ProjectContacts { get; set; } = new List<ProjectContact>();

        public static List<ClusterProject> ClusterProjects { get; set; } = new List<ClusterProject>();

        public static List<ClusterProjectCredential> ClusterProjectCredentials { get; set; } = new List<ClusterProjectCredential>();
    }
}
