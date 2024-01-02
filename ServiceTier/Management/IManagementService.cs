using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Models;
using System;

namespace HEAppE.ServiceTier.Management
{
    public interface IManagementService
    {
        CommandTemplateExt CreateCommandTemplate(long genericCommandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript, string sessionCode);
        CommandTemplateExt ModifyCommandTemplate(long commandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript, string sessionCode);
        void RemoveCommandTemplate(long commandTemplateId, string sessionCode);
        ProjectExt CreateProject(string accountingString, UsageType usageType, string name, string description, DateTime startDate, DateTime endDate, bool useAccountingStringForScheduler, string piEmail, string sessionCode);
        ProjectExt ModifyProject(long id, UsageType usageType, string name, string description, string accountingString, DateTime startDate, DateTime endDate, bool? useAccountingStringForScheduler, string sessionCode);
        void RemoveProject(long id, string sessionCode);
        ClusterProjectExt CreateProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath, string sessionCode);
        ClusterProjectExt ModifyProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath, string sessionCode);
        void RemoveProjectAssignmentToCluster(long projectId, long clusterId, string sessionCode);
        PublicKeyExt CreateSecureShellKey(string username, string password, long projectId, string sessionCode);
        PublicKeyExt RecreateSecureShellKey(string username, string password, string publicKey, long projectId, string sessionCode);
        void RemoveSecureShellKey(string publicKey, long projectId, string sessionCode);
        void InitializeClusterScriptDirectory(long projectId, string publicKey, string clusterProjectRootDirectory, string sessionCode);
        bool TestClusterAccessForAccount(long modelProjectId, string modelPublicKey, string modelSessionCode);
    }
}
