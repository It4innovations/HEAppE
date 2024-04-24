using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.ExtModels.ClusterInformation.Models;
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
        void InitializeClusterScriptDirectory(long projectId, string clusterProjectRootDirectory, string sessionCode);
        bool TestClusterAccessForAccount(long modelProjectId, string modelSessionCode, string username);

        ExtendedCommandTemplateParameterExt CreateCommandTemplateParameter(string modelIdentifier, string modelQuery,
            string modelDescription, long modelCommandTemplateId, string modelSessionCode);

        ExtendedCommandTemplateParameterExt ModifyCommandTemplateParameter(long modelId, string modelIdentifier,
            string modelQuery, string modelDescription, string modelSessionCode);

        string RemoveCommandTemplateParameter(long modelId, string modelSessionCode);
        List<ExtendedCommandTemplateExt> ListCommandTemplates(long projectId, string sessionCode);
        ExtendedCommandTemplateExt ListCommandTemplate(long commandTemplateId, string sessionCode);
        SubProjectExt ListSubProject(long subProjectId, string sessionCode);
        SubProjectExt CreateSubProject(long modelProjectId, string modelIdentifier, string modelDescription, DateTime modelStartDate, DateTime? modelEndDate, string modelSessionCode);
        SubProjectExt ModifySubProject(long modelId, string modelIdentifier, string modelDescription, DateTime modelStartDate, DateTime? modelEndDate, string modelSessionCode);
        void RemoveSubProject(long modelId, string modelSessionCode);
    }

}
