using System.Collections.Generic;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.OpenStack;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier;

public class MiddlewareContextSettings
{
    public static string ConnectionString { get; set; }
    public static List<AdaptorUser> AdaptorUsers { get; set; } = new();

    public static List<AdaptorUserRole> AdaptorUserRoles { get; set; } = new();

    public static List<AdaptorUserGroup> AdaptorUserGroups { get; set; } = new();

    public static List<AdaptorUserUserGroupRole> AdaptorUserUserGroupRoles { get; set; } = new();

    public static List<OpenStackInstance> OpenStackInstances { get; set; } = new();

    public static List<OpenStackDomain> OpenStackDomains { get; set; } = new();

    public static List<OpenStackProjectDomain> OpenStackProjectDomains { get; set; } = new();

    public static List<OpenStackProject> OpenStackProjects { get; set; } = new();

    public static List<OpenStackAuthenticationCredential> OpenStackAuthenticationCredentials { get; set; } = new();

    public static List<OpenStackAuthenticationCredentialDomain> OpenStackAuthenticationCredentialDomains { get; set; } =
        new();

    public static List<OpenStackAuthenticationCredentialProject>
        OpenStackAuthenticationCredentialProjects { get; set; } = new();

    public static List<ClusterProxyConnection> ClusterProxyConnections { get; set; } = new();

    public static List<Cluster> Clusters { get; set; } = new();

    public static List<ClusterAuthenticationCredentials> ClusterAuthenticationCredentials { get; set; } = new();

    public static List<ClusterNodeType> ClusterNodeTypes { get; set; } = new();

    public static List<CommandTemplate> CommandTemplates { get; set; } = new();

    public static List<CommandTemplateParameter> CommandTemplateParameters { get; set; } = new();

    public static List<FileTransferMethod> FileTransferMethods { get; set; } = new();

    public static List<Contact> Contacts { get; set; } = new();

    public static List<Project> Projects { get; set; } = new();
    public static List<SubProject> SubProjects { get; set; } = new();
    public static List<Accounting> Accountings { get; set; } = new();
    public static List<AccountingState> AccountingStates { get; set; } = new();
    public static List<ClusterNodeTypeAggregation> ClusterNodeTypeAggregations { get; set; } = new();
    public static List<ClusterNodeTypeAggregationAccounting> ClusterNodeTypeAggregationAccounting { get; set; } = new();
    public static List<ProjectClusterNodeTypeAggregation> ProjectClusterNodeTypeAggregations { get; set; } = new();
    public static List<ProjectContact> ProjectContacts { get; set; } = new();

    public static List<ClusterProject> ClusterProjects { get; set; } = new();

    public static List<ClusterProjectCredential> ClusterProjectCredentials { get; set; } = new();
}