using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.Management;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System;
using System.Collections.Generic;

namespace HEAppE.BusinessLogicTier.Logic.Management
{
    public interface IManagementLogic
    {
        CommandTemplate CreateCommandTemplate(long genericCommandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript);
        void RemoveCommandTemplate(long commandTemplateId);
        CommandTemplate ModifyCommandTemplate(long commandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript);
        SecureShellKey CreateSecureShellKey(string username, long[] projects);
        SecureShellKey RecreateSecureShellKey(string username, string publicKey);
        string RemoveSecureShellKey(string publicKey);
        Project CreateProject(string accountingString, UsageType usageType, string name, string description, DateTime startDate, DateTime endDate, AdaptorUser loggedUser);
        ClusterProject AssignProjectToCluster(long projectId, long clusterId, string localBasepath);
    }
}
