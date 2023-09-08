using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.Management.Models;
using System;

namespace HEAppE.ServiceTier.Management
{
    public interface IManagementService
    {
        CommandTemplateExt CreateCommandTemplate(long genericCommandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript, string sessionCode);
        PublicKeyExt CreateSecureShellKey(string username, long projectId, string sessionCode);
        PublicKeyExt RecreateSecureShellKey(string username, string publicKey, long projectId, string sessionCode);
        string RemoveSecureShellKey(string publicKey, long projectId, string sessionCode);
        CommandTemplateExt ModifyCommandTemplate(long commandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript, string sessionCode);
        string RemoveCommandTemplate(long commandTemplateId, string sessionCode);
        ProjectExt CreateProject(string accountingString, UsageType usageType, string name, string description, DateTime startDate, DateTime endDate, string sessionCode);
        ProjectExt ModifyProject(long id, UsageType usageType, string description, DateTime startDate, DateTime endDate, string sessionCode);
        string RemoveProject(long id, string sessionCode);
        ClusterProjectExt CreateProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath, string sessionCode);
        ClusterProjectExt ModifyProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath, string sessionCode);
        string RemoveProjectAssignmentToCluster(long projectId, long clusterId, string sessionCode);
    }
}
