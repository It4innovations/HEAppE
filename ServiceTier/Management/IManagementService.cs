using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Models;
using System;
using System.Collections.Generic;

namespace HEAppE.ServiceTier.Management
{
    public interface IManagementService
    {
        ExtendedCommandTemplateExt CreateCommandTemplateModel(string modelName, string modelDescription,
            string modelExtendedAllocationCommand, string modelExecutableFile, string modelPreparationScript,
            long modelProjectId, long modelClusterNodeTypeId, string modelSessionCode);

        CommandTemplateExt CreateCommandTemplateFromGeneric(long genericCommandTemplateId, string name, long projectId,
            string description, string extendedAllocationCommand, string executableFile, string preparationScript,
            string sessionCode);

        ExtendedCommandTemplateExt ModifyCommandTemplateModel(long modelId, string modelName, string modelDescription,
            string modelExtendedAllocationCommand, string modelExecutableFile, string modelPreparationScript,
            long modelClusterNodeTypeId, string modelSessionCode);

        CommandTemplateExt ModifyCommandTemplateFromGeneric(long commandTemplateId, string name, long projectId,
            string description, string extendedAllocationCommand, string executableFile, string preparationScript,
            string sessionCode);

        void RemoveCommandTemplate(long commandTemplateId, string sessionCode);

        ProjectExt CreateProject(string accountingString, UsageType usageType, string name, string description,
            DateTime startDate, DateTime endDate, bool useAccountingStringForScheduler, string piEmail,
            string sessionCode);

        ProjectExt ModifyProject(long id, UsageType usageType, string name, string description, DateTime startDate,
            DateTime endDate, bool? useAccountingStringForScheduler, string sessionCode);

        void RemoveProject(long id, string sessionCode);

        ClusterProjectExt CreateProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath,
            string sessionCode);

        ClusterProjectExt ModifyProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath,
            string sessionCode);

        void RemoveProjectAssignmentToCluster(long projectId, long clusterId, string sessionCode);

        List<PublicKeyExt> CreateSecureShellKey(IEnumerable<(string, string)> credentials, long projectId,
            string sessionCode);

        PublicKeyExt RegenerateSecureShellKey(string username, string password, string publicKey, long projectId,
            string sessionCode);

        void RemoveSecureShellKey(string username, string publicKey, long projectId, string sessionCode);
        List<ClusterInitReportExt> InitializeClusterScriptDirectory(long projectId, string clusterProjectRootDirectory, string sessionCode);
        bool TestClusterAccessForAccount(long modelProjectId, string modelSessionCode, string username);

        ExtendedCommandTemplateParameterExt CreateCommandTemplateParameter(string modelIdentifier, string modelQuery,
            string modelDescription, long modelCommandTemplateId, string modelSessionCode);

        ExtendedCommandTemplateParameterExt ModifyCommandTemplateParameter(long modelId, string modelIdentifier,
            string modelQuery, string modelDescription, string modelSessionCode);

        string RemoveCommandTemplateParameter(long modelId, string modelSessionCode);
        List<ExtendedCommandTemplateExt> ListCommandTemplates(long projectId, string sessionCode);
        ExtendedCommandTemplateExt ListCommandTemplate(long commandTemplateId, string sessionCode);
        SubProjectExt ListSubProject(long subProjectId, string sessionCode);
        List<SubProjectExt> ListSubProjects(long projectId, string sessionCode);
        SubProjectExt CreateSubProject(long modelProjectId, string modelIdentifier, string modelDescription, DateTime modelStartDate, DateTime? modelEndDate, string modelSessionCode);
        SubProjectExt ModifySubProject(long modelId, string modelIdentifier, string modelDescription, DateTime modelStartDate, DateTime? modelEndDate, string modelSessionCode);
        void RemoveSubProject(long modelId, string modelSessionCode);
        void ComputeAccounting(DateTime modelStartTime, DateTime modelEndTime, string modelSessionCode);
        ClusterExt GetClusterById(long clusterId, string sessionCode);
        ClusterExt CreateCluster(string name, string description, string masterNodeName, SchedulerType schedulerType, ClusterConnectionProtocol clusterConnectionProtocol,
            string timeZone, int port, bool updateJobStateByServiceAccount, string domainName, long? proxyConnectionId, string sessionCode);
        ClusterExt ModifyCluster(long id, string name, string description, string masterNodeName, SchedulerType schedulerType, ClusterConnectionProtocol clusterConnectionProtocol,
            string timeZone, int port, bool updateJobStateByServiceAccount, string domainName, long? proxyConnectionId, string sessionCode);
        void RemoveCluster(long id, string sessionCode);
        ClusterNodeTypeExt GetClusterNodeTypeById(long clusterId, string sessionCode);
        ClusterNodeTypeExt CreateClusterNodeType(string name, string description, int? numberOfNodes, int coresPerNode, string queue, string qualityOfService, int? maxWalltime,
            string clusterAllocationName, long? clusterId, long? fileTransferMethodId, long? clusterNodeTypeAggregationId, string sessionCode);
        ClusterNodeTypeExt ModifyClusterNodeType(long id, string name, string description, int? numberOfNodes, int coresPerNode, string queue, string qualityOfService,
            int? maxWalltime, string clusterAllocationName, long? clusterId, long? fileTransferMethodId, long? clusterNodeTypeAggregationId, string sessionCode);
        void RemoveClusterNodeType(long id, string sessionCode);
        ClusterProxyConnectionExt GetClusterProxyConnectionById(long clusterProxyConnectionId, string sessionCode);
        ClusterProxyConnectionExt CreateClusterProxyConnection(string host, int port, string username, string password, ProxyType type, string sessionCode);
        ClusterProxyConnectionExt ModifyClusterProxyConnection(long id, string host, int port, string username, string password, ProxyType type, string sessionCode);
        void RemoveClusterProxyConnection(long id, string sessionCode);
        FileTransferMethodExt GetFileTransferMethodById(long fileTransferMethodId, string sessionCode);
        FileTransferMethodExt CreateFileTransferMethod(string serverHostname, FileTransferProtocol protocol, long clusterId, int? port, string sessionCode);
        FileTransferMethodExt ModifyFileTransferMethod(long id, string serverHostname, FileTransferProtocol protocol, long clusterId, int? port, string sessionCode);
        void RemoveFileTransferMethod(long id, string sessionCode);
        ClusterNodeTypeAggregationExt GetClusterNodeTypeAggregationById(long id, string sessionCode);
        List<ClusterNodeTypeAggregationExt> GetClusterNodeTypeAggregations(string sessionCode);
        ClusterNodeTypeAggregationExt CreateClusterNodeTypeAggregation(string name, string description, string allocationType, DateTime validityFrom,
            DateTime? validityTo, string sessionCode);
        ClusterNodeTypeAggregationExt ModifyClusterNodeTypeAggregation(long id, string name, string description, string allocationType, DateTime validityFrom,
            DateTime? validityTo, string sessionCode);
        void RemoveClusterNodeTypeAggregation(long id, string sessionCode);
        ClusterNodeTypeAggregationAccountingExt GetClusterNodeTypeAggregationAccountingById(long clusterNodeTypeAggregationId, long accountingId, string sessionCode);
        ClusterNodeTypeAggregationAccountingExt CreateClusterNodeTypeAggregationAccounting(long clusterNodeTypeAggregationId, long accountingId, string sessionCode);
        void RemoveClusterNodeTypeAggregationAccounting(long clusterNodeTypeAggregationId, long accountingId, string sessionCode);
    }
}
