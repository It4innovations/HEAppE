using System;
using System.Collections.Generic;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Models;
using System.Threading.Tasks;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;

namespace HEAppE.ServiceTier.Management;

public interface IManagementService
{
    ExtendedCommandTemplateExt CreateCommandTemplateModel(string modelName, string modelDescription,
        string modelExtendedAllocationCommand, string modelExecutableFile, string modelPreparationScript,
        long modelProjectId, long modelClusterNodeTypeId, string modelSessionCode);

    Task<CommandTemplateExt> CreateCommandTemplateFromGeneric(long genericCommandTemplateId, string name,
        long projectId,
        string description, string extendedAllocationCommand, string executableFile, string preparationScript,
        string sessionCode);

    ExtendedCommandTemplateExt ModifyCommandTemplateModel(long modelId, string modelName, string modelDescription,
        string modelExtendedAllocationCommand, string modelExecutableFile, string modelPreparationScript,
        long modelClusterNodeTypeId, bool modelIsEnabled, string modelSessionCode);

    Task<CommandTemplateExt> ModifyCommandTemplateFromGeneric(long commandTemplateId, string name, long projectId,
        string description, string extendedAllocationCommand, string executableFile, string preparationScript,
        string sessionCode);

    void RemoveCommandTemplate(long commandTemplateId, string sessionCode);

    List<ProjectExt> ListProjects(string sessionCode);

    ProjectExt GetProjectByAccountingString(string accountingString, string sessionCode);

    ProjectExt GetProjectById(long id, string sessionCode);

    ProjectExt CreateProject(string accountingString, UsageType usageType, string name, string description,
        DateTime startDate, DateTime endDate, bool useAccountingStringForScheduler, string piEmail, bool isOneToOneMapping,
        string sessionCode);

    ProjectExt ModifyProject(long id, UsageType usageType, string name, string description, DateTime startDate,
        DateTime endDate, bool? useAccountingStringForScheduler, bool isOneToOneMapping, string sessionCode);

    void RemoveProject(long id, string sessionCode);

    ClusterProjectExt GetProjectAssignmentToClusterById(long projectId, long clusterId, string sessionCode);
    ClusterProjectExt[] GetProjectAssignmentToClusters(long projectId, string sessionCode);

    ClusterProjectExt CreateProjectAssignmentToCluster(long projectId, long clusterId, string scratchStoragePath, string projectStoragePath,
        string sessionCode);

    ClusterProjectExt ModifyProjectAssignmentToCluster(long projectId, long clusterId, string scratchStoragePath, string projectStoragePath,
        string sessionCode);

    void RemoveProjectAssignmentToCluster(long projectId, long clusterId, string sessionCode);

    Task<List<PublicKeyExt>> GetSecureShellKeys(long projectId, string sessionCode);

    Task<List<PublicKeyExt>> CreateSecureShellKey(IEnumerable<(string, string)> credentials, long projectId,
        string sessionCode);

    Task<PublicKeyExt> RegenerateSecureShellKey(string username, string password, string publicKey, long projectId,
        string sessionCode);

    Task RemoveSecureShellKey(string username, string publicKey, long projectId, string sessionCode);

    public Task<List<ClusterInitReportExt>> InitializeClusterScriptDirectory(long projectId,
        bool overwriteExistingProjectRootDirectory, string sessionCode, string username);

    public Task<List<ClusterAccessReportExt>> TestClusterAccessForAccount(long modelProjectId, string modelSessionCode,
        string username);
    public Task<List<ClusterAccountStatusExt>> ClusterAccountStatus(long modelProjectId, string modelSessionCode,
        string username);

    ExtendedCommandTemplateParameterExt GetCommandTemplateParameterById(long id, string modelSessionCode);

    ExtendedCommandTemplateParameterExt CreateCommandTemplateParameter(string modelIdentifier, string modelQuery,
        string modelDescription, long modelCommandTemplateId, string modelSessionCode, bool isVisible = true);

    ExtendedCommandTemplateParameterExt ModifyCommandTemplateParameter(long id, string modelIdentifier,
        string modelQuery, string modelDescription, string modelSessionCode);

    string RemoveCommandTemplateParameter(long id, string modelSessionCode);
    List<ExtendedCommandTemplateExt> ListCommandTemplates(long projectId, string sessionCode);
    ExtendedCommandTemplateExt ListCommandTemplate(long commandTemplateId, string sessionCode);
    SubProjectExt ListSubProject(long subProjectId, string sessionCode);
    List<SubProjectExt> ListSubProjects(long projectId, string sessionCode);

    SubProjectExt CreateSubProject(long modelProjectId, string modelIdentifier, string modelDescription,
        DateTime modelStartDate, DateTime? modelEndDate, string modelSessionCode);

    SubProjectExt ModifySubProject(long modelId, string modelIdentifier, string modelDescription,
        DateTime modelStartDate, DateTime? modelEndDate, string modelSessionCode);

    void RemoveSubProject(long modelId, string modelSessionCode);
    ExtendedClusterExt GetClusterById(long clusterId, string sessionCode);
    List<ExtendedClusterExt> GetClusters(string sessionCode);

    ExtendedClusterExt CreateCluster(string name, string description, string masterNodeName, SchedulerType schedulerType,
        ClusterConnectionProtocol clusterConnectionProtocol,
        string timeZone, int? port, bool updateJobStateByServiceAccount, string domainName, long? proxyConnectionId,
        string sessionCode);

    ExtendedClusterExt ModifyCluster(long id, string name, string description, string masterNodeName,
        SchedulerType schedulerType, ClusterConnectionProtocol clusterConnectionProtocol,
        string timeZone, int? port, bool updateJobStateByServiceAccount, string domainName, long? proxyConnectionId,
        string sessionCode);

    void RemoveCluster(long id, string sessionCode);

    List<ClusterNodeTypeExt> ListClusterNodeTypes(string sessionCode);

    ClusterNodeTypeExt GetClusterNodeTypeById(long clusterId, string sessionCode);

    ClusterNodeTypeExt CreateClusterNodeType(string name, string description, int? numberOfNodes, int coresPerNode,
        string queue, string qualityOfService, int? maxWalltime,
        string clusterAllocationName, long? clusterId, long? fileTransferMethodId, long? clusterNodeTypeAggregationId,
        string sessionCode);

    ClusterNodeTypeExt ModifyClusterNodeType(long id, string name, string description, int? numberOfNodes,
        int coresPerNode, string queue, string qualityOfService,
        int? maxWalltime, string clusterAllocationName, long? clusterId, long? fileTransferMethodId,
        long? clusterNodeTypeAggregationId, string sessionCode);

    void RemoveClusterNodeType(long id, string sessionCode);
    ClusterProxyConnectionExt GetClusterProxyConnectionById(long clusterProxyConnectionId, string sessionCode);
    List<ClusterProxyConnectionExt> GetClusterProxyConnections(string sessionCode);

    ClusterProxyConnectionExt CreateClusterProxyConnection(string host, int port, string username, string password,
        ProxyType type, string sessionCode);

    ClusterProxyConnectionExt ModifyClusterProxyConnection(long id, string host, int port, string username,
        string password, ProxyType type, string sessionCode);

    void RemoveClusterProxyConnection(long id, string sessionCode);

    List<FileTransferMethodNoCredentialsExt> ListFileTransferMethods(string sessionCode);

    FileTransferMethodNoCredentialsExt GetFileTransferMethodById(long fileTransferMethodId, string sessionCode);

    FileTransferMethodNoCredentialsExt CreateFileTransferMethod(string serverHostname, FileTransferProtocol protocol, long clusterId,
        int? port, string sessionCode);

    FileTransferMethodNoCredentialsExt ModifyFileTransferMethod(long id, string serverHostname, FileTransferProtocol protocol,
        long clusterId, int? port, string sessionCode);

    void RemoveFileTransferMethod(long id, string sessionCode);
    ClusterNodeTypeAggregationExt GetClusterNodeTypeAggregationById(long id, string sessionCode);
    List<ClusterNodeTypeAggregationExt> GetClusterNodeTypeAggregations(string sessionCode);

    ClusterNodeTypeAggregationExt CreateClusterNodeTypeAggregation(string name, string description,
        string allocationType, DateTime validityFrom,
        DateTime? validityTo, string sessionCode);

    ClusterNodeTypeAggregationExt ModifyClusterNodeTypeAggregation(long id, string name, string description,
        string allocationType, DateTime validityFrom,
        DateTime? validityTo, string sessionCode);

    void RemoveClusterNodeTypeAggregation(long id, string sessionCode);

    List<ClusterNodeTypeAggregationAccountingExt> ListClusterNodeTypeAggregationAccountings(string sessionCode);

    ClusterNodeTypeAggregationAccountingExt GetClusterNodeTypeAggregationAccountingById(
        long clusterNodeTypeAggregationId, long accountingId, string sessionCode);

    ClusterNodeTypeAggregationAccountingExt CreateClusterNodeTypeAggregationAccounting(
        long clusterNodeTypeAggregationId, long accountingId, string sessionCode);

    void RemoveClusterNodeTypeAggregationAccounting(long clusterNodeTypeAggregationId, long accountingId,
        string sessionCode);

    List<AccountingExt> ListAccountings(string sessionCode);

    AccountingExt GetAccountingById(long id, string sessionCode);

    AccountingExt CreateAccounting(string formula, DateTime validityFrom, DateTime? validityTo, string sessionCode);

    AccountingExt ModifyAccounting(long id, string formula, DateTime validityFrom, DateTime? validityTo,
        string sessionCode);

    void RemoveAccounting(long id, string sessionCode);

    ProjectClusterNodeTypeAggregationExt GetProjectClusterNodeTypeAggregationById(long projectId,
        long clusterNodeTypeAggregationId, string sessionCode);

    List<ProjectClusterNodeTypeAggregationExt> GetProjectClusterNodeTypeAggregations(
        string sessionCode);
    List<ProjectClusterNodeTypeAggregationExt> GetProjectClusterNodeTypeAggregationsByProjectId(long projectId,
        string sessionCode);

    ProjectClusterNodeTypeAggregationExt CreateProjectClusterNodeTypeAggregation(long projectId,
        long clusterNodeTypeAggregationId, long allocationAmount, string sessionCode);

    ProjectClusterNodeTypeAggregationExt ModifyProjectClusterNodeTypeAggregation(long projectId,
        long clusterNodeTypeAggregationId, long allocationAmount, string sessionCode);

    void RemoveProjectClusterNodeTypeAggregation(long projectId, long clusterNodeTypeAggregationId, string sessionCode);
    void ComputeAccounting(DateTime modelStartTime, DateTime modelEndTime, long projectId, string modelSessionCode);
    List<AccountingStateExt> ListAccountingStates(long projectId, string sessionCode);
    string BackupDatabase(string sessionCode);
    string BackupDatabaseTransactionLogs(string sessionCode);
    List<DatabaseBackupExt> ListDatabaseBackups(DateTime? fromDateTime, DateTime? toDateTime, DatabaseBackupTypeExt? type, string sessionCode);
    void RestoreDatabase(string backupFileName, bool includeLogs, string sessionCode);
    public Task<List<PublicKeyExt>> ModifyClusterAuthenticationCredential(string oldUsername, string newUsername,
        string newPassword, long projectId,
        string sessionCode);

    Task<StatusExt> Status(long projectId, DateTime? timeFrom, DateTime? timeTo, string sessionCode);

    StatusCheckLogsExt StatusErrorLogs(long projectId, DateTime? timeFrom, DateTime? timeTo, string sessionCode);
    AdaptorUserCreatedExt CreateAdaptorUser(string username, object sessionCode);
    AdaptorUserCreatedExt ModifyAdaptorUser(string oldUsername, string newUsername, string modelSessionCode);
    string DeleteAdaptorUser(string modelUsername, string modelSessionCode);
    AdaptorUserExt GetAdaptorUserByUsername(string username, string sessionCode);
    AdaptorUserExt AssignAdaptorUserToProject(string modelUsername, long modelProjectId, AdaptorUserRoleType modelRole, string modelSessionCode);
    AdaptorUserExt RemoveAdaptorUserFromProject(string modelUsername, long modelProjectId, AdaptorUserRoleType modelRole, string modelSessionCode);
    AdaptorUserExt[] ListAdaptorUsersInProject(long projectId, string sessionCode);
    ExtendedCommandTemplateExt CreateGenericCommandTemplate(string modelName, string modelDescription, string modelExtendedAllocationCommand, string modelPreparationScript, long modelProjectId, long modelClusterNodeTypeId, string modelSessionCode);
    ExtendedCommandTemplateExt ModifyGenericCommandTemplate(long modelId, string modelName, string modelDescription, string modelExtendedAllocationCommand, string modelPreparationScript, long modelClusterNodeTypeId, bool modelIsEnabled, string modelSessionCode);
}
