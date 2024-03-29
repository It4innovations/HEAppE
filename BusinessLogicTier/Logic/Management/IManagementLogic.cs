﻿using HEAppE.DomainObjects.JobManagement;
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
        CommandTemplate ModifyCommandTemplate(long commandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript);
        void RemoveCommandTemplate(long commandTemplateId);
        DomainObjects.JobManagement.Project CreateProject(string accountingString, UsageType usageType, string name, string description, DateTime startDate, DateTime endDate, bool useAccountingStringForScheduler, string piEmail, AdaptorUser loggedUser);
        DomainObjects.JobManagement.Project ModifyProject(long id, UsageType usageType, string modelName, string description, DateTime startDate, DateTime endDate, bool? useAccountingStringForScheduler);
        void RemoveProject(long id);
        List<SecureShellKey> CreateSecureShellKey(IEnumerable<(string, string)> credentials, long projectId);
        SecureShellKey RegenerateSecureShellKey(string username, string password, long projectId);
        void RemoveSecureShellKey(string publicKey, long projectId);
        SecureShellKey RegenerateSecureShellKeyByPublicKey(string publicKey, string password, long projectId);
        void RemoveSecureShellKeyByPublicKey(string publicKey, long projectId);
        ClusterProject CreateProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath);
        ClusterProject ModifyProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath);
        void RemoveProjectAssignmentToCluster(long projectId, long clusterId);
        void InitializeClusterScriptDirectory(long projectId, string clusterProjectRootDirectory);
        bool TestClusterAccessForAccount(long projectId, string username);
    }
}
