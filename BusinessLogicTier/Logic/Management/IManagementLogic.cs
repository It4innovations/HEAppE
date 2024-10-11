﻿using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.Management;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System;
using System.Collections.Generic;

namespace HEAppE.BusinessLogicTier.Logic.Management
{
    public interface IManagementLogic
    {
        CommandTemplate CreateCommandTemplate(string modelName, string modelDescription, string modelExtendedAllocationCommand, string modelExecutableFile, string modelPreparationScript, long modelProjectId, long modelClusterNodeTypeId);
        CommandTemplate CreateCommandTemplateFromGeneric(long genericCommandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript);
        CommandTemplate ModifyCommandTemplate(long modelId, string modelName, string modelDescription, string modelExtendedAllocationCommand, string modelExecutableFile, string modelPreparationScript, long modelClusterNodeTypeId);
        CommandTemplate ModifyCommandTemplateFromGeneric(long commandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript);
        void RemoveCommandTemplate(long commandTemplateId);
        Project GetProjectById(long id);
        Project CreateProject(string accountingString, UsageType usageType, string name, string description, DateTime startDate, DateTime endDate, bool useAccountingStringForScheduler, string piEmail, AdaptorUser loggedUser);
        Project ModifyProject(long id, UsageType usageType, string modelName, string description, DateTime startDate, DateTime endDate, bool? useAccountingStringForScheduler);
        void RemoveProject(long id);
        List<SecureShellKey> CreateSecureShellKey(IEnumerable<(string, string)> credentials, long projectId);
        SecureShellKey RegenerateSecureShellKey(string username, string password, long projectId);
        void RemoveSecureShellKey(string publicKey, long projectId);
        SecureShellKey RegenerateSecureShellKeyByPublicKey(string publicKey, string password, long projectId);
        void RemoveSecureShellKeyByPublicKey(string publicKey, long projectId);
        ClusterProject GetProjectAssignmentToClusterById(long projectId, long clusterId);
        ClusterProject CreateProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath);
        ClusterProject ModifyProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath);
        void RemoveProjectAssignmentToCluster(long projectId, long clusterId);
        List<ClusterInitReport> InitializeClusterScriptDirectory(long projectId, string clusterProjectRootDirectory);
        bool TestClusterAccessForAccount(long projectId, string username);
        CommandTemplateParameter GetCommandTemplateParameterById(long id);
        CommandTemplateParameter CreateCommandTemplateParameter(string modelIdentifier, string modelQuery,
            string modelDescription, long modelCommandTemplateId);
        CommandTemplateParameter ModifyCommandTemplateParameter(long id, string modelIdentifier, string modelQuery, string modelDescription);
        void RemoveCommandTemplateParameter(long id);
        List<CommandTemplate> ListCommandTemplates(long projectId);
        SubProject CreateSubProject(string identifier, long projectId);
        SubProject CreateSubProject(long modelProjectId, string modelIdentifier, string modelDescription, DateTime modelStartDate, DateTime? modelEndDate);
        SubProject ModifySubProject(long modelId, string modelIdentifier, string modelDescription, DateTime modelStartDate, DateTime? modelEndDate);
        void RemoveSubProject(long modelId);
        void ComputeAccounting(DateTime modelStartTime, DateTime modelEndTime, long projectId);
        Cluster GetClusterById(long clusterId);
        Cluster CreateCluster(string name, string description, string masterNodeName, SchedulerType schedulerType, ClusterConnectionProtocol clusterConnectionProtocol,
            string timeZone, int port, bool updateJobStateByServiceAccount, string domainName, long? proxyConnectionId);
        Cluster ModifyCluster(long id, string name, string description, string masterNodeName, SchedulerType schedulerType, ClusterConnectionProtocol clusterConnectionProtocol,
            string timeZone, int port, bool updateJobStateByServiceAccount, string domainName, long? proxyConnectionId);
        void RemoveCluster(long id);
        ClusterNodeType GetClusterNodeTypeById(long id);
        ClusterNodeType CreateClusterNodeType(string name, string description, int? numberOfNodes, int coresPerNode, string queue, string qualityOfService, int? maxWalltime,
            string clusterAllocationName, long? clusterId, long? fileTransferMethodId, long? clusterNodeTypeAggregationId);
        ClusterNodeType ModifyClusterNodeType(long id, string name, string description, int? numberOfNodes, int coresPerNode, string queue, string qualityOfService, int? maxWalltime,
            string clusterAllocationName, long? clusterId, long? fileTransferMethodId, long? clusterNodeTypeAggregationId);
        void RemoveClusterNodeType(long id);
        ClusterProxyConnection GetClusterProxyConnectionById(long id);
        ClusterProxyConnection CreateClusterProxyConnection(string host, int port, string username, string password, ProxyType type);
        ClusterProxyConnection ModifyClusterProxyConnection(long id, string host, int port, string username, string password, ProxyType type);
        void RemoveClusterProxyConnection(long id);
        FileTransferMethod GetFileTransferMethodById(long id);
        FileTransferMethod CreateFileTransferMethod(string serverHostname, FileTransferProtocol protocol, long clusterId, int? port);
        FileTransferMethod ModifyFileTransferMethod(long id, string serverHostname, FileTransferProtocol protocol, long clusterId, int? port);
        void RemoveFileTransferMethod(long id);
        ClusterNodeTypeAggregation GetClusterNodeTypeAggregationById(long id);
        List<ClusterNodeTypeAggregation> GetClusterNodeTypeAggregations();
        ClusterNodeTypeAggregation CreateClusterNodeTypeAggregation(string name, string description, string allocationType, DateTime validityFrom, DateTime? validityTo);
        ClusterNodeTypeAggregation ModifyClusterNodeTypeAggregation(long id, string name, string description, string allocationType, DateTime validityFrom, DateTime? validityTo);
        void RemoveClusterNodeTypeAggregation(long id);
        ClusterNodeTypeAggregationAccounting GetClusterNodeTypeAggregationAccountingById(long clusterNodeTypeAggregationId, long accountingId);
        ClusterNodeTypeAggregationAccounting CreateClusterNodeTypeAggregationAccounting(long clusterNodeTypeAggregationId, long accountingId);
        void RemoveClusterNodeTypeAggregationAccounting(long clusterNodeTypeAggregationId, long accountingId);
        Accounting GetAccountingById(long id);
        Accounting CreateAccounting(string formula, DateTime validityFrom);
        Accounting ModifyAccounting(long id, string formula, DateTime validityFrom, DateTime? validityTo);
        void RemoveAccounting(long id);
        ProjectClusterNodeTypeAggregation GetProjectClusterNodeTypeAggregationById(long projectId, long clusterNodeTypeAggregationId);
        List<ProjectClusterNodeTypeAggregation> GetProjectClusterNodeTypeAggregationsByProjectId(long projectId);
        ProjectClusterNodeTypeAggregation CreateProjectClusterNodeTypeAggregation(long projectId, long clusterNodeTypeAggregationId, long allocationAmount);
        ProjectClusterNodeTypeAggregation ModifyProjectClusterNodeTypeAggregation(long projectId, long clusterNodeTypeAggregationId, long allocationAmount);
        void RemoveProjectClusterNodeTypeAggregation(long projectId, long clusterNodeTypeAggregationId);
        List<AccountingState> ListAccountingStates(long projectId);
    }
}
