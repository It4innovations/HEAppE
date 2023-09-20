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
        void RemoveCommandTemplate(long commandTemplateId);
        CommandTemplate ModifyCommandTemplate(long commandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript);
        SecureShellKey CreateSecureShellKey(string username, string password, long projectId);
        SecureShellKey RecreateSecureShellKey(string username, string password, string publicKey, long projectId);
        string RemoveSecureShellKey(string publicKey, long projectId);
        Project CreateProject(string accountingString, UsageType usageType, string name, string description, DateTime startDate, DateTime endDate, AdaptorUser loggedUser);
        Project ModifyProject(long id, UsageType usageType, string description, DateTime startDate, DateTime endDate);
        string RemoveProject(long id);
        ClusterProject CreateProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath);
        ClusterProject ModifyProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath);
        string RemoveProjectAssignmentToCluster(long projectId, long clusterId);
        string InitializeClusterScriptDirectory(long projectId, string publicKey, string clusterProjectRootDirectory);
    }
}
