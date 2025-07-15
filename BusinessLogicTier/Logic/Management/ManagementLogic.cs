using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Transactions;
using HEAppE.CertificateGenerator;
using HEAppE.CertificateGenerator.Configuration;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DataAccessTier.Vault;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.Management;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.Utils;
using log4net;
using Org.BouncyCastle.Security;

namespace HEAppE.BusinessLogicTier.Logic.Management;

public class ManagementLogic : IManagementLogic
{
    protected static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    ///     Script Configuration
    /// </summary>
    protected readonly ScriptsConfiguration _scripts = HPCConnectionFrameworkConfiguration.ScriptsSettings;

    protected IUnitOfWork _unitOfWork;

    public ManagementLogic(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    /// <summary>
    ///     Create a command template based on a generic command template
    /// </summary>
    /// <param name="genericCommandTemplateId"></param>
    /// <param name="name"></param>
    /// <param name="projectId"></param>
    /// <param name="description"></param>
    /// <param name="extendedAllocationCommand"></param>
    /// <param name="executableFile"></param>
    /// <param name="preparationScript"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    /// <exception cref="InputValidationException"></exception>
    public CommandTemplate CreateCommandTemplateFromGeneric(long genericCommandTemplateId, string name,
        long projectId, string description, string extendedAllocationCommand, string executableFile,
        string preparationScript, long? adaptorUserId)
    {
        var project = _unitOfWork.ProjectRepository.GetById(projectId) ??
                      throw new RequestedObjectDoesNotExistException("ProjectNotFound");

        var commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(genericCommandTemplateId) ??
                              throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");

        if (!commandTemplate.IsGeneric) throw new InputValidationException("CommandTemplateNotGeneric");

        var commandTemplateParameter =
            commandTemplate.TemplateParameters.FirstOrDefault(f => f.IsVisible);
        if (string.IsNullOrEmpty(commandTemplateParameter?.Identifier))
            throw new RequestedObjectDoesNotExistException("UserScriptNotDefined");

        if (string.IsNullOrEmpty(executableFile)) throw new InputValidationException("NoScriptPath");

        var cluster = commandTemplate.ClusterNodeType.Cluster;
        var serviceAccount =
            _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(cluster.Id,
                projectId, adaptorUserId: adaptorUserId);
        var commandTemplateParameters = SchedulerFactory.GetInstance(cluster.SchedulerType)
            .CreateScheduler(cluster, project, adaptorUserId: adaptorUserId)
            .GetParametersFromGenericUserScript(cluster, serviceAccount, executableFile)
            .ToList();

        List<CommandTemplateParameter> templateParameters = new();
        foreach (var parameter in commandTemplateParameters)
            templateParameters.Add(new CommandTemplateParameter
            {
                Identifier = parameter,
                Description = parameter,
                Query = string.Empty
            });

        CommandTemplate newCommandTemplate = new()
        {
            Name = name,
            Description = description,
            IsGeneric = false,
            IsEnabled = true,
            IsDeleted = false,
            Project = project,
            ClusterNodeType = commandTemplate.ClusterNodeType,
            ClusterNodeTypeId = commandTemplate.ClusterNodeTypeId,
            ExtendedAllocationCommand = extendedAllocationCommand,
            ExecutableFile = executableFile,
            PreparationScript = preparationScript,
            TemplateParameters = templateParameters,
            CommandParameters = string.Join(' ', commandTemplateParameters.Select(x => $"%%{"{"}{x}{"}"}")),
            CreatedFromId = commandTemplate.CreatedFromId,
            CreatedFrom = commandTemplate,
            CreatedAt = DateTime.UtcNow
        };

        _logger.Info($"Creating new command template: {newCommandTemplate.Name}");
        _unitOfWork.CommandTemplateRepository.Insert(newCommandTemplate);
        _unitOfWork.Save();

        return newCommandTemplate;
    }

    public CommandTemplate CreateCommandTemplate(string modelName, string modelDescription,
        string modelExtendedAllocationCommand,
        string modelExecutableFile, string modelPreparationScript, long modelProjectId, long modelClusterNodeTypeId)
    {
        var clusterNodeType = _unitOfWork.ClusterNodeTypeRepository.GetById(modelClusterNodeTypeId) ??
                              throw new RequestedObjectDoesNotExistException("ClusterNodeTypeNotExists");

        var clusterProject =
            _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterNodeType.ClusterId.Value,
                modelProjectId) ??
            throw new RequestedObjectDoesNotExistException("ClusterProjectCombinationNotFound", modelClusterNodeTypeId,
                modelProjectId);

        var project = clusterProject.Project;

        CommandTemplate commandTemplate = new()
        {
            Name = modelName,
            Description = modelDescription,
            IsGeneric = false,
            IsEnabled = true,
            IsDeleted = false,
            Project = project,
            ProjectId = project.Id,
            ClusterNodeType = clusterNodeType,
            ClusterNodeTypeId = clusterNodeType.Id,
            ExtendedAllocationCommand = modelExtendedAllocationCommand,
            ExecutableFile = modelExecutableFile,
            PreparationScript = modelPreparationScript,
            TemplateParameters = new List<CommandTemplateParameter>(),
            CommandParameters = string.Empty,
            CreatedAt = DateTime.UtcNow,
            CreatedFrom = null
        };

        _logger.Info($"Creating new command template: {commandTemplate}");
        _unitOfWork.CommandTemplateRepository.Insert(commandTemplate);
        _unitOfWork.Save();

        return commandTemplate;
    }

    public CommandTemplate ModifyCommandTemplate(long modelId, string modelName, string modelDescription,
        string modelExtendedAllocationCommand, string modelExecutableFile, string modelPreparationScript,
        long modelClusterNodeTypeId, bool modelIsEnabled)
    {
        var commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(modelId) ??
                              throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");

        if (commandTemplate.IsDeleted) throw new InputValidationException("CommandTemplateDeleted");

        if (commandTemplate.CreatedFrom is not null) throw new InvalidRequestException("CommandTemplateNotStatic");

        var project = commandTemplate.Project ??
                      throw new InvalidRequestException("NotPermitted");

        var clusterNodeType = _unitOfWork.ClusterNodeTypeRepository.GetById(modelClusterNodeTypeId) ??
                              throw new RequestedObjectDoesNotExistException("ClusterNodeTypeNotExists");


        _ = _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterNodeType.ClusterId.Value,
                project.Id) ??
            throw new RequestedObjectDoesNotExistException("ClusterProjectCombinationNotFound", modelClusterNodeTypeId,
                project.Id);

        commandTemplate.IsEnabled = modelIsEnabled;
        commandTemplate.Name = modelName;
        commandTemplate.Description = modelDescription;
        commandTemplate.ExtendedAllocationCommand = modelExtendedAllocationCommand;
        commandTemplate.ExecutableFile = modelExecutableFile;
        commandTemplate.PreparationScript = modelPreparationScript;
        commandTemplate.ClusterNodeType = clusterNodeType;
        commandTemplate.ClusterNodeTypeId = clusterNodeType.Id;
        commandTemplate.ModifiedAt = DateTime.UtcNow;

        _logger.Info($"Modifying command template: {commandTemplate}");
        _unitOfWork.Save();

        return commandTemplate;
    }

    /// <summary>
    ///     Modify command template
    /// </summary>
    /// <param name="commandTemplateId"></param>
    /// <param name="name"></param>
    /// <param name="projectId"></param>
    /// <param name="description"></param>
    /// <param name="extendedAllocationCommand"></param>
    /// <param name="executableFile"></param>
    /// <param name="preparationScript"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    /// <exception cref="InputValidationException"></exception>
    public CommandTemplate ModifyCommandTemplateFromGeneric(long commandTemplateId, string name, long projectId,
        string description, string extendedAllocationCommand, string executableFile, string preparationScript, long? adaptorUserId)
    {
        var commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId) ??
                              throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");

        if (commandTemplate.CreatedFrom is null) throw new InvalidRequestException("CommandTemplateNotFromGeneric");

        var project = _unitOfWork.ProjectRepository.GetById(projectId) ??
                      throw new RequestedObjectDoesNotExistException("ProjectNotFound");

        if (commandTemplate.IsGeneric) throw new InputValidationException("CommandTemplateIsGeneric");

        if (executableFile is null) throw new InputValidationException("CommandTemplateNoExecutableFile");

        var cluster = commandTemplate.ClusterNodeType.Cluster;
        var serviceAccount =
            _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(cluster.Id, projectId, adaptorUserId: adaptorUserId);
        var commandTemplateParameters = SchedulerFactory.GetInstance(cluster.SchedulerType)
            .CreateScheduler(cluster, project, adaptorUserId: adaptorUserId)
            .GetParametersFromGenericUserScript(cluster, serviceAccount, executableFile)
            .ToList();

        List<CommandTemplateParameter> templateParameters = new();
        foreach (var parameter in commandTemplateParameters)
            templateParameters.Add(new CommandTemplateParameter
            {
                Identifier = parameter,
                Description = parameter,
                Query = string.Empty
            });

        _logger.Info($"Modifying command template: {commandTemplate.Name}");
        commandTemplate.Name = name;
        commandTemplate.Description = description;
        commandTemplate.ExtendedAllocationCommand = extendedAllocationCommand;
        commandTemplate.PreparationScript = preparationScript;
        commandTemplate.TemplateParameters.ForEach(_unitOfWork.CommandTemplateParameterRepository.Delete);
        commandTemplate.TemplateParameters.AddRange(templateParameters);
        commandTemplate.ExecutableFile = executableFile;
        commandTemplate.CommandParameters = string.Join(' ', commandTemplateParameters.Select(x => $"%%{"{"}{x}{"}"}"));
        commandTemplate.ModifiedAt = DateTime.UtcNow;

        _logger.Info($"Modifying command template: {commandTemplate}");
        _unitOfWork.Save();
        return commandTemplate;
    }

    /// <summary>
    ///     Removes the specified command template from the database
    /// </summary>
    /// <param name="commandTemplateId"></param>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public void RemoveCommandTemplate(long commandTemplateId)
    {
        var commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId)
                              ?? throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");

        _logger.Info($"Removing command template: {commandTemplate}");
        commandTemplate.IsDeleted = true;
        _unitOfWork.Save();
    }

    public Project GetProjectByAccountingString(string accountingString)
    {
        return _unitOfWork.ProjectRepository.GetByAccountingString(accountingString) ??
               throw new RequestedObjectDoesNotExistException("ProjectNotFound");
    }

    /// <summary>
    ///     Get Project by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Project GetProjectById(long id)
    {
        return _unitOfWork.ProjectRepository.GetById(id) ??
               throw new RequestedObjectDoesNotExistException("ProjectNotFound");
    }

    /// <summary>
    ///     Creates a new project in the database and returns it
    /// </summary>
    /// <param name="accountingString"></param>
    /// <param name="usageType"></param>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <param name="loggedUser"></param>
    /// <returns></returns>
    /// <exception cref="InputValidationException"></exception>
    public Project CreateProject(string accountingString, UsageType usageType, string name, string description,
        DateTime startDate, DateTime endDate, bool useAccountingStringForScheduler, string piEmail, bool isOneToOneMapping,
        AdaptorUser loggedUser)
    {
        var existingProject = _unitOfWork.ProjectRepository.GetByAccountingString(accountingString);
        if (existingProject != null) throw new InputValidationException("ProjectAlreadyExist");

        var contact = _unitOfWork.ContactRepository.GetByEmail(piEmail)
                      ?? new Contact
                      {
                          Email = piEmail
                      };

        var project = InitializeProject(accountingString, usageType, name, description, startDate, endDate,
            useAccountingStringForScheduler, contact, isOneToOneMapping);

        // Create user groups for different purposes
        var defaultAdaptorUserGroup = CreateAdaptorUserGroup(project, name, description, string.Empty);
        var lexisAdaptorUserGroup = CreateAdaptorUserGroup(project, name, description,
            LexisAuthenticationConfiguration.HEAppEGroupNamePrefix);
        var openIdAdaptorUserGroup =
            CreateAdaptorUserGroup(project, name, description, ExternalAuthConfiguration.HEAppEUserPrefix);

        using (TransactionScope transactionScope = new(
                   TransactionScopeOption.Required,
                   new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
        {
            _unitOfWork.AdaptorUserGroupRepository.Insert(defaultAdaptorUserGroup);
            _unitOfWork.AdaptorUserGroupRepository.Insert(lexisAdaptorUserGroup);
            _unitOfWork.AdaptorUserGroupRepository.Insert(openIdAdaptorUserGroup);

            try
            {
                _unitOfWork.Save();
            }
            //catch unique constraing to AccountingString 
            catch (Exception ex) when (ex.InnerException is not null &&
                                       ex.InnerException.Message.Contains("IX_Project_AccountingString"))
            {
                throw new InputValidationException("ProjectAlreadyExist");
            }


            // Check if an admin user exists
            var heappeAdminUser = _unitOfWork.AdaptorUserRepository.GetById(1);
            if (heappeAdminUser != null)
            {           
                heappeAdminUser.CreateSpecificUserRoleForUser(defaultAdaptorUserGroup,
                    AdaptorUserRoleType.Administrator);
                _unitOfWork.AdaptorUserRepository.Update(heappeAdminUser);
                _unitOfWork.Save();
            }

            var adaptorUserGroup = loggedUser.UserType switch
            {
                AdaptorUserType.Default => defaultAdaptorUserGroup,
                AdaptorUserType.OpenId => openIdAdaptorUserGroup,
                AdaptorUserType.Lexis => lexisAdaptorUserGroup,
                _ => defaultAdaptorUserGroup
            };

            loggedUser.CreateSpecificUserRoleForUser(adaptorUserGroup, AdaptorUserRoleType.ManagementAdmin);
            _unitOfWork.AdaptorUserRepository.Update(loggedUser);
            _unitOfWork.Save();

            _logger.Info($"Created project with id {project.Id}.");
            transactionScope.Complete();
        }

        return project;
    }

    /// <summary>
    ///     Modifies an existing project in the database and returns it
    /// </summary>
    /// <param name="id"></param>
    /// <param name="usageType"></param>
    /// <param name="modelName"></param>
    /// <param name="description"></param>
    /// <param name="accountingString"></param>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public Project ModifyProject(long id, UsageType usageType, string modelName, string description, DateTime startDate,
        DateTime endDate, bool? useAccountingStringForScheduler, bool isOneToOneMapping)
    {
        var project = _unitOfWork.ProjectRepository.GetById(id)
                      ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound");

        project.UsageType = usageType;
        project.Name = modelName ?? project.Name;
        project.Description = description ?? project.Description;
        project.StartDate = startDate;
        project.EndDate = endDate;
        project.ModifiedAt = DateTime.UtcNow;
        project.UseAccountingStringForScheduler =
            useAccountingStringForScheduler ?? project.UseAccountingStringForScheduler;
        project.IsOneToOneMapping = isOneToOneMapping;

        _unitOfWork.ProjectRepository.Update(project);
        _unitOfWork.Save();
        _logger.Info($"Project ID '{project.Id}' has been modified.");

        return project;
    }

    /// <summary>
    ///     Removes project from the database
    /// </summary>
    /// <param name="id"></param>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public void RemoveProject(long id)
    {
        var project = _unitOfWork.ProjectRepository.GetById(id)
                      ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound");

        var modified = DateTime.UtcNow;
        project.IsDeleted = true;
        project.ModifiedAt = modified;
        project.ClusterProjects.ForEach(x => RemoveProjectAssignmentToCluster(x.ProjectId, x.ClusterId));
        project.ProjectClusterNodeTypeAggregations.ForEach(x =>
        {
            x.ModifiedAt = modified;
            x.IsDeleted = true;
        });

        _unitOfWork.ProjectRepository.Update(project);
        _logger.Info($"Project id '{project.Id}' has been deleted.");
        _unitOfWork.Save();
    }

    /// <summary>
    ///     Get project to cluster assignment by id
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="clusterId"></param>
    /// <returns></returns>
    public ClusterProject GetProjectAssignmentToClusterById(long projectId, long clusterId)
    {
        return _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterId, projectId)
               ?? throw new InputValidationException("ProjectNoReferenceToCluster", projectId, clusterId);
    }

    /// <summary>
    ///     Assigns a project to a clusters
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="clusterId"></param>
    /// <param name="localBasepath"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    /// <exception cref="InputValidationException"></exception>
    public ClusterProject CreateProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath)
    {
        var project = _unitOfWork.ProjectRepository.GetById(projectId) ??
                      throw new RequestedObjectDoesNotExistException("ProjectNotFound");
        _ = _unitOfWork.ClusterRepository.GetById(clusterId) ??
            throw new RequestedObjectDoesNotExistException("ClusterNotExists", clusterId);
        var cp = _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterId, projectId);

        if (cp != null)
            //Cluster to project is marked as deleted, update it
            return !cp.IsDeleted
                ? throw new InputValidationException("ProjectAlreadyExistWithCluster")
                : ModifyProjectAssignmentToCluster(projectId, clusterId, localBasepath);


        //Create cluster to project mapping
        var modified = DateTime.UtcNow;
        ClusterProject clusterProject = new()
        {
            ClusterId = clusterId,
            ProjectId = projectId,
            LocalBasepath = localBasepath
                .Replace(_scripts.SubExecutionsPath, string.Empty, true, CultureInfo.InvariantCulture)
                .TrimEnd('\\', '/'),
            CreatedAt = modified,
            IsDeleted = false
        };

        project.ModifiedAt = modified;
        _unitOfWork.ProjectRepository.Update(project);
        _unitOfWork.ClusterProjectRepository.Insert(clusterProject);
        _unitOfWork.Save();

        _logger.Info($"Created Project ID '{projectId} assignment to Cluster ID '{clusterId}'.");
        return clusterProject;
    }

    /// <summary>
    ///     Modifies a project assignment to a clusters
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="clusterId"></param>
    /// <param name="localBasepath"></param>
    /// <returns></returns>
    /// <exception cref="InputValidationException"></exception>
    public ClusterProject ModifyProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath)
    {
        var clusterProject =
            _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterId, projectId)
            ?? throw new InputValidationException("ProjectNoReferenceToCluster", projectId, clusterId);

        var modified = DateTime.UtcNow;
        clusterProject.LocalBasepath = localBasepath
            .Replace(_scripts.SubExecutionsPath, string.Empty, true, CultureInfo.InvariantCulture).TrimEnd('\\', '/');
        clusterProject.ModifiedAt = modified;
        clusterProject.IsDeleted = false;
        clusterProject.Project.ModifiedAt = modified;
        _unitOfWork.ProjectRepository.Update(clusterProject.Project);
        _unitOfWork.ClusterProjectRepository.Update(clusterProject);
        _unitOfWork.Save();

        _logger.Info($"Project ID '{projectId}' assignment to Cluster ID '{clusterId}' was modified.");
        return clusterProject;
    }

    /// <summary>
    ///     Removes a project assignment to a cluster
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="clusterId"></param>
    /// <exception cref="InputValidationException"></exception>
    public void RemoveProjectAssignmentToCluster(long projectId, long clusterId)
    {
        var clusterProject =
            _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterId, projectId)
            ?? throw new InputValidationException("ProjectNoReferenceToCluster", projectId, clusterId);

        var modified = DateTime.UtcNow;
        clusterProject.IsDeleted = true;
        clusterProject.ModifiedAt = modified;
        clusterProject.ClusterProjectCredentials.ForEach(x =>
        {
            x.IsDeleted = true;
            x.ModifiedAt = modified;
            x.ClusterAuthenticationCredentials.IsDeleted = true;
        });

        if(clusterProject.Project is null)
        {
            _logger.Info($"Project with ID '{projectId}' not found for Cluster ID '{clusterId}' while deleting ProjectAssignmentToCluster reference.");
        }
        else
        {
            clusterProject.Project.ModifiedAt = modified;
            _unitOfWork.ProjectRepository.Update(clusterProject.Project);
        }
        
        _unitOfWork.ClusterProjectRepository.Update(clusterProject);
        _unitOfWork.Save();

        _logger.Info($"Removed assignment of the Project with ID '{projectId}' to the Cluster ID '{clusterId}'");
    }

    /// <summary>
    ///     Returns a list of SSH keys for the specified project
    /// </summary>
    /// <param name="projectId"></param>
    /// <returns></returns>
    public List<SecureShellKey> GetSecureShellKeys(long projectId, long? adaptorUserId)
    {
        var project = _unitOfWork.ProjectRepository.GetById(projectId);
        if (project is null || project.EndDate < DateTime.UtcNow)
        {
            _logger.Error($"Project with ID {projectId} not found or has already ended.");
            throw new RequestedObjectDoesNotExistException("ProjectNotFound");
        }

        if (project.IsOneToOneMapping)
        {
            _logger.Info($"Project with ID {projectId} is one-to-one mapping, returning only service account credentials for user {adaptorUserId}.");
        }
        else
        {
            _logger.Info($"Project with ID {projectId} is not one-to-one mapping, returning all SSH keys for project.");
        }
        
        return _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAuthenticationCredentialsProject(projectId, adaptorUserId: adaptorUserId)
            .Where(x => !x.IsDeleted && x.IsGenerated && !string.IsNullOrEmpty(x.PrivateKey))
            .Select(SSHGenerator.GetPublicKeyFromPrivateKey)
            .DistinctBy(x=>x.Username)
            .ToList();
    }

    /// <summary>
    ///     Creates encrypted SSH key for the specified user and saves it to the database.
    /// </summary>
    /// <param name="credentials"></param>
    /// <param name="projectId"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public List<SecureShellKey> CreateSecureShellKey(IEnumerable<(string, string)> credentials, long projectId, long? adaptorUserId)
    {
        var project = _unitOfWork.ProjectRepository.GetById(projectId);
        if (project is null || project.EndDate < DateTime.UtcNow)
            throw new RequestedObjectDoesNotExistException("ProjectNotFound");
        List<SecureShellKey> secureShellKeys = new();
        foreach ((var username, var password) in credentials)
        {
            var existingCredentials =
                _unitOfWork.ClusterAuthenticationCredentialsRepository
                    .GetAuthenticationCredentialsForUsernameAndProject(username, projectId, adaptorUserId: adaptorUserId);
            if (existingCredentials.Any())
            {
                //get existing secure key
                var existingKey = existingCredentials.FirstOrDefault();
                if (existingKey != null && string.IsNullOrEmpty(existingKey.PrivateKey)) continue;

                if (existingKey != null)
                {
                    //get PUBLIC KEY FROM PRIVATE KEY
                    var sshKey = SSHGenerator.GetPublicKeyFromPrivateKey(existingKey);
                    secureShellKeys.Add(sshKey);
                    continue;
                }
            }

            secureShellKeys.Add(CreateSecureShellKey(username, password, project, adaptorUserId));
        }

        return secureShellKeys;
    }

    /// <summary>
    ///     Recreates encrypted SSH key for the specified user and saves it to the database.
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="projectId"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public SecureShellKey RegenerateSecureShellKey(string username, string password, long projectId)
    {
        var clusterAuthenticationCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAll().Where(
            w => w.Username == username &&
                 w.AuthenticationType != ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent &&
                 w.ClusterProjectCredentials.Any(a => a.ClusterProject.ProjectId == projectId));

        if (!clusterAuthenticationCredentials.Any()) throw new InvalidRequestException("HPCIdentityNotFound");

        _logger.Info($"Recreating SSH key for user {username}.");

        var modificationDate = DateTime.UtcNow;
        SSHGenerator sshGenerator = new();
        var passphrase = StringUtils.GetRandomString();
        var secureShellKey = sshGenerator.GetEncryptedSecureShellKey(username, passphrase);

        foreach (var credentials in clusterAuthenticationCredentials)
        {
            credentials.PrivateKey = secureShellKey.PrivateKeyPEM;
            credentials.PrivateKeyPassphrase = passphrase;
            credentials.PublicKeyFingerprint = secureShellKey.PublicKeyFingerprint;
            credentials.CipherType = secureShellKey.CipherType;
            credentials.ClusterProjectCredentials.ForEach(cpc =>
            {
                cpc.IsDeleted = false;
                cpc.ModifiedAt = modificationDate;
                cpc.ClusterProject.Project.ModifiedAt = modificationDate;
            });
            _unitOfWork.ClusterAuthenticationCredentialsRepository.Update(credentials);
        }

        _unitOfWork.Save();
        return secureShellKey;
    }

    /// <summary>
    ///     Removes encrypted SSH key
    /// </summary>
    /// <param name="username"></param>
    /// <param name="projectId"></param>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public void RemoveSecureShellKey(string username, long projectId)
    {
        var clusterAuthenticationCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAll().Where(
            w => w.Username == username &&
                 w.AuthenticationType != ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent &&
                 w.ClusterProjectCredentials.Any(a => a.ClusterProject.ProjectId == projectId));

        if (!clusterAuthenticationCredentials.Any()) throw new InvalidRequestException("HPCIdentityNotFound");


        var modificationDate = DateTime.UtcNow;
        _logger.Info($"Removing SSH key for user {clusterAuthenticationCredentials.First().Username}.");
        foreach (var credentials in clusterAuthenticationCredentials)
        {
            credentials.IsDeleted = true;
            credentials.ClusterProjectCredentials.ForEach(cpc =>
            {
                cpc.IsDeleted = true;
                cpc.ModifiedAt = modificationDate;
                cpc.ClusterProject.Project.ModifiedAt = modificationDate;
            });
            _unitOfWork.ClusterAuthenticationCredentialsRepository.Update(credentials);
        }

        _unitOfWork.Save();
    }

    /// <summary>
    ///     Recreates encrypted SSH key for the specified user and saves it to the database.
    /// </summary>
    /// <param name="publicKey"></param>
    /// <param name="password"></param>
    /// <param name="projectId"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    [Obsolete]
    public SecureShellKey RegenerateSecureShellKeyByPublicKey(string publicKey, string password, long projectId)
    {
        var publicKeyFingerprint = ComputePublicKeyFingerprint(publicKey);
        var clusterAuthenticationCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository
            .GetAllGeneratedWithFingerprint(publicKeyFingerprint, projectId)
            .ToList();
        if (!clusterAuthenticationCredentials.Any())
            throw new RequestedObjectDoesNotExistException("PublicKeyNotFound");

        var username = clusterAuthenticationCredentials.First().Username;

        _logger.Info($"Recreating SSH key for user {username}.");

        var modificationDate = DateTime.UtcNow;
        SSHGenerator sshGenerator = new();
        var passphrase = StringUtils.GetRandomString();
        var secureShellKey = sshGenerator.GetEncryptedSecureShellKey(username, passphrase);

        foreach (var credentials in clusterAuthenticationCredentials)
        {
            credentials.PrivateKey = secureShellKey.PrivateKeyPEM;
            credentials.PrivateKeyPassphrase = passphrase;
            credentials.PublicKeyFingerprint = secureShellKey.PublicKeyFingerprint;
            credentials.CipherType = secureShellKey.CipherType;
            credentials.ClusterProjectCredentials.ForEach(cpc =>
            {
                cpc.IsDeleted = false;
                cpc.ModifiedAt = modificationDate;
                cpc.ClusterProject.Project.ModifiedAt = modificationDate;
            });
            _unitOfWork.ClusterAuthenticationCredentialsRepository.Update(credentials);
        }

        _unitOfWork.Save();
        return secureShellKey;
    }

    /// <summary>
    ///     Removes encrypted SSH key
    /// </summary>
    /// <param name="publicKey"></param>
    /// <param name="projectId"></param>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    [Obsolete]
    public void RemoveSecureShellKeyByPublicKey(string publicKey, long projectId)
    {
        var publicKeyFingerprint = ComputePublicKeyFingerprint(publicKey);
        var clusterAuthenticationCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository
            .GetAllGeneratedWithFingerprint(publicKeyFingerprint, projectId)
            .ToList();

        if (!clusterAuthenticationCredentials.Any())
            throw new RequestedObjectDoesNotExistException("PublicKeyNotFound");

        var modificationDate = DateTime.UtcNow;
        _logger.Info($"Removing SSH key for user {clusterAuthenticationCredentials.First().Username}.");
        foreach (var credentials in clusterAuthenticationCredentials)
        {
            credentials.IsDeleted = true;
            credentials.ClusterProjectCredentials.ForEach(cpc =>
            {
                cpc.IsDeleted = true;
                cpc.ModifiedAt = modificationDate;
                cpc.ClusterProject.Project.ModifiedAt = modificationDate;
            });
            _unitOfWork.ClusterAuthenticationCredentialsRepository.Update(credentials);
        }

        _unitOfWork.Save();
    }

    /// <summary>
    ///     Initialize cluster script directory and create symbolic link for user
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="overwriteExistingProjectRootDirectory"></param>
    ///     /// <param name="adaptorUserId"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public List<ClusterInitReport> InitializeClusterScriptDirectory(long projectId, bool overwriteExistingProjectRootDirectory, long? adaptorUserId)
    {
        var clusterAuthenticationCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository
            .GetAuthenticationCredentialsProject(projectId, adaptorUserId: adaptorUserId)
            .ToList();
        Dictionary<Cluster, ClusterInitReport> clusterInitReports = new();

        if (!clusterAuthenticationCredentials.Any())
            throw new RequestedObjectDoesNotExistException("NotExistingPublicKey");

        foreach (var clusterAuthCredentials in clusterAuthenticationCredentials.DistinctBy(x => x.Username))
        foreach (var clusterProjectCredential in clusterAuthCredentials.ClusterProjectCredentials.DistinctBy(x =>
                     x.ClusterProject))
        {
            var cluster = clusterProjectCredential.ClusterProject.Cluster;
            var project = clusterProjectCredential.ClusterProject.Project;
            var localBasepath = clusterProjectCredential.ClusterProject.LocalBasepath;
            var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, project, adaptorUserId: adaptorUserId);
            var isInitialized = scheduler.InitializeClusterScriptDirectory(project.AccountingString, overwriteExistingProjectRootDirectory,
                localBasepath, cluster, clusterAuthCredentials, clusterProjectCredential.IsServiceAccount);
            if (isInitialized)
            {
                if (!clusterInitReports.ContainsKey(cluster))
                    clusterInitReports.Add(cluster, new ClusterInitReport
                    {
                        Cluster = cluster,
                        NumberOfInitializedAccounts = 1
                    });
                else
                    clusterInitReports[cluster].NumberOfInitializedAccounts++;
                _logger.Info(
                    $"Initialized cluster script directory for project {project.Id} on cluster {cluster.Id} with account {clusterAuthCredentials.Username}.");
            }
            else
            {
                if (!clusterInitReports.ContainsKey(cluster))
                    clusterInitReports.Add(cluster, new ClusterInitReport
                    {
                        Cluster = cluster,
                        NumberOfNotInitializedAccounts = 1
                    });
                else
                    clusterInitReports[cluster].NumberOfNotInitializedAccounts++;
                _logger.Error(
                    $"Initialization of cluster script directory failed for project {project.Id} on cluster {cluster.Id} with account {clusterProjectCredential.ClusterAuthenticationCredentials.Username}.");
            }
        }

        return clusterInitReports.Values.ToList();
    }

    /// <summary>
    ///     Test cluster access for robot account
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="username"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public bool TestClusterAccessForAccount(long projectId, string username)
    {
        var clusterAuthenticationCredentials = string.IsNullOrEmpty(username)
            ? _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAllGenerated(projectId).ToList()
            : _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAll().Where(w =>
                w.Username == username &&
                w.AuthenticationType != ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent &&
                w.ClusterProjectCredentials.Any(a => a.ClusterProject.ProjectId == projectId));

        if (!clusterAuthenticationCredentials.Any()) throw new InvalidRequestException("HPCIdentityNotFound");

        List<long> noAccessClusterIds = new();
        foreach (var clusterAuthCredentials in clusterAuthenticationCredentials.DistinctBy(x => x.Username))
        foreach (var clusterProjectCredential in clusterAuthCredentials.ClusterProjectCredentials.DistinctBy(x =>
                     x.ClusterProject))
        {
            var cluster = clusterProjectCredential.ClusterProject.Cluster;
            var project = clusterProjectCredential.ClusterProject.Project;

            var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, project, adaptorUserId: null);
            if (!scheduler.TestClusterAccessForAccount(cluster, clusterAuthCredentials))
                noAccessClusterIds.Add(cluster.Id);
        }

        return !noAccessClusterIds.Any();
    }
    
    

    /// <summary>
    ///     Get GetCommandTemplateParameter by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public CommandTemplateParameter GetCommandTemplateParameterById(long id)
    {
        return _unitOfWork.CommandTemplateParameterRepository.GetById(id)
               ?? throw new RequestedObjectDoesNotExistException("CommandTemplateParameterNotFound", id);
    }

    /// <summary>
    ///     Create command template parameter
    /// </summary>
    /// <param name="modelIdentifier"></param>
    /// <param name="modelQuery"></param>
    /// <param name="modelDescription"></param>
    /// <param name="modelCommandTemplateId"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    /// <exception cref="InputValidationException"></exception>
    /// <exception cref="InvalidRequestException"></exception>
    public CommandTemplateParameter CreateCommandTemplateParameter(string modelIdentifier, string modelQuery,
        string modelDescription, long modelCommandTemplateId)
    {
        var commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(modelCommandTemplateId);
        if (commandTemplate is null) throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");

        //if is not static
        if (commandTemplate.CreatedFrom is not null) throw new InvalidRequestException("CommandTemplateNotStatic");

        //if identifier already exists in command template
        if (commandTemplate.TemplateParameters.Exists(x => x.Identifier == modelIdentifier))
            throw new InputValidationException("CommandTemplateAlreadyParameterExists");

        CommandTemplateParameter commandTemplateParameter = new()
        {
            Identifier = modelIdentifier,
            Query = modelQuery,
            Description = modelDescription,
            CommandTemplate = commandTemplate,
            CommandTemplateId = commandTemplate.Id,
            CreatedAt = DateTime.UtcNow
        };

        _unitOfWork.CommandTemplateParameterRepository.Insert(commandTemplateParameter);
        AddCommandTemplateParameterToCommandTemplate(commandTemplate, commandTemplateParameter);
        _unitOfWork.Save();

        return commandTemplateParameter;
    }

    /// <summary>
    ///     Modify command template parameter
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="modelIdentifier"></param>
    /// <param name="modelQuery"></param>
    /// <param name="modelDescription"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    /// <exception cref="InputValidationException"></exception>
    /// <exception cref="InvalidRequestException"></exception>
    public CommandTemplateParameter ModifyCommandTemplateParameter(long id, string modelIdentifier, string modelQuery,
        string modelDescription)
    {
        var commandTemplateParameter = _unitOfWork.CommandTemplateParameterRepository.GetById(id);
        if (commandTemplateParameter is null)
            throw new RequestedObjectDoesNotExistException("CommandTemplateParameterNotFound", id);

        if (commandTemplateParameter.CommandTemplate.IsDeleted)
            throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");

        //if is not static
        if (commandTemplateParameter.CommandTemplate.CreatedFrom is not null)
            throw new InvalidRequestException("CommandTemplateNotStatic");

        //if identifier already exists in command template
        if (!commandTemplateParameter.CommandTemplate.TemplateParameters.Exists(x => x.Identifier == modelIdentifier))
        {
            var previousIdentifier = commandTemplateParameter.Identifier;
            commandTemplateParameter.Identifier = modelIdentifier;
            ModifyCommandTemplateParameterFromCommandTemplate(commandTemplateParameter.CommandTemplate,
                commandTemplateParameter, previousIdentifier);
        }

        commandTemplateParameter.Query = modelQuery;
        commandTemplateParameter.Description = modelDescription;
        commandTemplateParameter.ModifiedAt = DateTime.UtcNow;

        _unitOfWork.CommandTemplateParameterRepository.Update(commandTemplateParameter);
        _unitOfWork.Save();

        return commandTemplateParameter;
    }

    /// <summary>
    ///     Remove command template parameter
    /// </summary>
    /// <param name="modelId"></param>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    /// <exception cref="InputValidationException"></exception>
    /// <exception cref="InvalidRequestException"></exception>
    public void RemoveCommandTemplateParameter(long id)
    {
        var commandTemplateParameter = _unitOfWork.CommandTemplateParameterRepository.GetById(id);
        if (commandTemplateParameter is null)
            throw new RequestedObjectDoesNotExistException("CommandTemplateParameterNotFound", id);

        if (commandTemplateParameter.CommandTemplate.IsDeleted)
            throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");

        //if is not static
        if (commandTemplateParameter.CommandTemplate.CreatedFrom is not null)
            throw new InvalidRequestException("CommandTemplateNotStatic");

        RemoveCommandTemplateParameterFromCommandTemplate(commandTemplateParameter.CommandTemplate,
            commandTemplateParameter);
        commandTemplateParameter.IsEnabled = false;
        commandTemplateParameter.ModifiedAt = DateTime.UtcNow;
        commandTemplateParameter.IsVisible = false;
        _unitOfWork.Save();
    }

    /// <summary>
    ///     List command templates
    /// </summary>
    /// <param name="projectId"></param>
    /// <returns></returns>
    public List<CommandTemplate> ListCommandTemplates(long projectId)
    {
        return _unitOfWork.CommandTemplateRepository.GetCommandTemplatesByProjectId(projectId)
            .Where(x => !x.IsDeleted)
            .ToList();
    }

    /// <summary>
    ///     Get cluster by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public Cluster GetClusterById(long id)
    {
        var cluster = _unitOfWork.ClusterRepository.GetById(id);

        return cluster ?? throw new RequestedObjectDoesNotExistException("ClusterNotExists", id);
    }
    
    public Cluster GetByIdWithProxyConnection(long id)
    {
        var cluster = _unitOfWork.ClusterRepository.GetByIdWithProxyConnection(id);
        return cluster ?? throw new RequestedObjectDoesNotExistException("ClusterNotExists", id);
    }

    /// <summary>
    ///     Creates a new cluster in the database and returns it
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="masterNodeName"></param>
    /// <param name="schedulerType"></param>
    /// <param name="clusterConnectionProtocol"></param>
    /// <param name="timeZone"></param>
    /// <param name="port"></param>
    /// <param name="updateJobStateByServiceAccount"></param>
    /// <param name="domainName"></param>
    /// <param name="proxyConnectionId"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public Cluster CreateCluster(string name, string description, string masterNodeName, SchedulerType schedulerType,
        ClusterConnectionProtocol clusterConnectionProtocol,
        string timeZone, int? port, bool updateJobStateByServiceAccount, string domainName, long? proxyConnectionId)
    {
        if (proxyConnectionId.HasValue)
            _ = _unitOfWork.ClusterProxyConnectionRepository.GetById((long)proxyConnectionId) ??
                throw new RequestedObjectDoesNotExistException("ProxyConnectionNotFound", proxyConnectionId);

        var cluster = new Cluster
        {
            Name = name,
            Description = description,
            MasterNodeName = masterNodeName,
            SchedulerType = schedulerType,
            ConnectionProtocol = clusterConnectionProtocol,
            TimeZone = timeZone,
            Port = port,
            UpdateJobStateByServiceAccount = updateJobStateByServiceAccount,
            DomainName = domainName,
            ProxyConnectionId = proxyConnectionId
        };
        _unitOfWork.ClusterRepository.Insert(cluster);
        _unitOfWork.Save();

        return cluster;
    }

    /// <summary>
    ///     Modify existing cluster
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="masterNodeName"></param>
    /// <param name="schedulerType"></param>
    /// <param name="clusterConnectionProtocol"></param>
    /// <param name="timeZone"></param>
    /// <param name="port"></param>
    /// <param name="updateJobStateByServiceAccount"></param>
    /// <param name="domainName"></param>
    /// <param name="proxyConnectionId"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public Cluster ModifyCluster(long id, string name, string description, string masterNodeName,
        SchedulerType schedulerType, ClusterConnectionProtocol clusterConnectionProtocol,
        string timeZone, int? port, bool updateJobStateByServiceAccount, string domainName, long? proxyConnectionId)
    {
        var existingCluster = _unitOfWork.ClusterRepository.GetById(id) ??
                              throw new RequestedObjectDoesNotExistException("ClusterNotExists", id);
        if (proxyConnectionId.HasValue)
            _ = _unitOfWork.ClusterProxyConnectionRepository.GetById((long)proxyConnectionId) ??
                throw new RequestedObjectDoesNotExistException("ProxyConnectionNotFound", proxyConnectionId);

        existingCluster.Name = name;
        existingCluster.Description = description;
        existingCluster.MasterNodeName = masterNodeName;
        existingCluster.SchedulerType = schedulerType;
        existingCluster.ConnectionProtocol = clusterConnectionProtocol;
        existingCluster.TimeZone = timeZone;
        existingCluster.Port = port;
        existingCluster.UpdateJobStateByServiceAccount = updateJobStateByServiceAccount;
        existingCluster.DomainName = domainName;
        existingCluster.ProxyConnectionId = proxyConnectionId;
        _unitOfWork.ClusterRepository.Update(existingCluster);
        _unitOfWork.Save();

        return existingCluster;
    }

    /// <summary>
    ///     Remove existing cluster
    /// </summary>
    /// <param name="id"></param>
    public void RemoveCluster(long id)
    {
        var existingCluster = _unitOfWork.ClusterRepository.GetById(id) ??
                              throw new RequestedObjectDoesNotExistException("ClusterNotExists", id);

        // remove cluster project references
        existingCluster.ClusterProjects.ForEach(x => RemoveProjectAssignmentToCluster(x.ProjectId, x.ClusterId));
        // soft delete cluster node types
        existingCluster.NodeTypes.ToList().ForEach(nt => RemoveClusterNodeType(nt.Id));
        // TODO - delete job specification?
        // soft delete file transfer methods
        existingCluster.FileTransferMethods.ToList().ForEach(ftm => RemoveFileTransferMethod(ftm.Id));
        // soft delete proxy connection
        if (existingCluster.ProxyConnection != null)
            RemoveClusterProxyConnection((long)existingCluster.ProxyConnectionId);
        // soft delete cluster
        existingCluster.IsDeleted = true;
        _unitOfWork.ClusterRepository.Update(existingCluster);
        _unitOfWork.Save();
    }

    /// <summary>
    ///     Get ClusterNodeType by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public ClusterNodeType GetClusterNodeTypeById(long id)
    {
        return _unitOfWork.ClusterNodeTypeRepository.GetById(id) ??
               throw new RequestedObjectDoesNotExistException("ClusterNodeTypeNotExists", id);
    }

    /// <summary>
    ///     Create ClusterNodeType
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="numberOfNodes"></param>
    /// <param name="coresPerNode"></param>
    /// <param name="queue"></param>
    /// <param name="qualityOfService"></param>
    /// <param name="maxWalltime"></param>
    /// <param name="clusterAllocationName"></param>
    /// <param name="clusterId"></param>
    /// <param name="fileTransferMethodId"></param>
    /// <param name="clusterNodeTypeAggregationId"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public ClusterNodeType CreateClusterNodeType(string name, string description, int? numberOfNodes, int coresPerNode,
        string queue, string qualityOfService, int? maxWalltime,
        string clusterAllocationName, long? clusterId, long? fileTransferMethodId, long? clusterNodeTypeAggregationId)
    {
        if (clusterId.HasValue)
            _ = _unitOfWork.ClusterRepository.GetById((long)clusterId) ??
                throw new RequestedObjectDoesNotExistException("ClusterNotFound", clusterId);
        if (fileTransferMethodId.HasValue)
            _ = _unitOfWork.FileTransferMethodRepository.GetById((long)fileTransferMethodId) ??
                throw new RequestedObjectDoesNotExistException("FileTransferMethodNotFound", fileTransferMethodId);
        if (clusterNodeTypeAggregationId.HasValue)
            _ = _unitOfWork.ClusterNodeTypeAggregationRepository.GetById((long)clusterNodeTypeAggregationId) ??
                throw new RequestedObjectDoesNotExistException("ClusterNodeTypeAggregationNotFound",
                    clusterNodeTypeAggregationId);

        var clusterNodeType = new ClusterNodeType
        {
            Name = name,
            Description = description,
            NumberOfNodes = numberOfNodes,
            CoresPerNode = coresPerNode,
            Queue = queue,
            QualityOfService = qualityOfService,
            MaxWalltime = maxWalltime,
            ClusterAllocationName = clusterAllocationName,
            ClusterId = clusterId,
            FileTransferMethodId = fileTransferMethodId,
            ClusterNodeTypeAggregationId = clusterNodeTypeAggregationId
        };
        _unitOfWork.ClusterNodeTypeRepository.Insert(clusterNodeType);
        _unitOfWork.Save();

        return clusterNodeType;
    }

    /// <summary>
    ///     Modify ClusterNodeType
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="numberOfNodes"></param>
    /// <param name="coresPerNode"></param>
    /// <param name="queue"></param>
    /// <param name="qualityOfService"></param>
    /// <param name="maxWalltime"></param>
    /// <param name="clusterAllocationName"></param>
    /// <param name="clusterId"></param>
    /// <param name="fileTransferMethodId"></param>
    /// <param name="clusterNodeTypeAggregationId"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public ClusterNodeType ModifyClusterNodeType(long id, string name, string description, int? numberOfNodes,
        int coresPerNode, string queue, string qualityOfService,
        int? maxWalltime, string clusterAllocationName, long? clusterId, long? fileTransferMethodId,
        long? clusterNodeTypeAggregationId)
    {
        var existingClusterNodeType = _unitOfWork.ClusterNodeTypeRepository.GetById(id) ??
                                      throw new RequestedObjectDoesNotExistException("ClusterNodeTypeNotExists", id);

        if (clusterId.HasValue)
            _ = _unitOfWork.ClusterRepository.GetById((long)clusterId) ??
                throw new RequestedObjectDoesNotExistException("ClusterNotFound", clusterId);
        if (fileTransferMethodId.HasValue)
            _ = _unitOfWork.FileTransferMethodRepository.GetById((long)fileTransferMethodId) ??
                throw new RequestedObjectDoesNotExistException("FileTransferMethodNotFound", fileTransferMethodId);
        if (clusterNodeTypeAggregationId.HasValue)
            _ = _unitOfWork.ClusterNodeTypeAggregationRepository.GetById((long)clusterNodeTypeAggregationId) ??
                throw new RequestedObjectDoesNotExistException("ClusterNodeTypeAggregationNotFound",
                    clusterNodeTypeAggregationId);

        existingClusterNodeType.Name = name;
        existingClusterNodeType.Description = description;
        existingClusterNodeType.NumberOfNodes = numberOfNodes;
        existingClusterNodeType.CoresPerNode = coresPerNode;
        existingClusterNodeType.Queue = queue;
        existingClusterNodeType.QualityOfService = qualityOfService;
        existingClusterNodeType.MaxWalltime = maxWalltime;
        existingClusterNodeType.ClusterAllocationName = clusterAllocationName;
        existingClusterNodeType.ClusterId = clusterId;
        existingClusterNodeType.FileTransferMethodId = fileTransferMethodId;
        existingClusterNodeType.ClusterNodeTypeAggregationId = clusterNodeTypeAggregationId;
        _unitOfWork.ClusterNodeTypeRepository.Update(existingClusterNodeType);
        _unitOfWork.Save();

        return existingClusterNodeType;
    }

    /// <summary>
    ///     Remove ClusterNodeType
    /// </summary>
    /// <param name="id"></param>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public void RemoveClusterNodeType(long id)
    {
        var existingClusterNodeType = _unitOfWork.ClusterNodeTypeRepository.GetById(id) ??
                                      throw new RequestedObjectDoesNotExistException("ClusterNodeTypeNotExists", id);

        // delete cluster node type requested groups
        existingClusterNodeType.RequestedNodeGroups.ForEach(_unitOfWork.ClusterNodeTypeRequestedGroupRepository.Delete);
        // remove command templates
        existingClusterNodeType.PossibleCommands.ForEach(c => RemoveCommandTemplate(c.Id));
        // soft delete cluster node type
        existingClusterNodeType.IsDeleted = true;
        _unitOfWork.ClusterNodeTypeRepository.Update(existingClusterNodeType);
        _unitOfWork.Save();
    }

    /// <summary>
    ///     Get ClusterProxyConnection by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public ClusterProxyConnection GetClusterProxyConnectionById(long id)
    {
        var clusterProxyConnection = _unitOfWork.ClusterProxyConnectionRepository.GetById(id);

        return clusterProxyConnection ??
               throw new RequestedObjectDoesNotExistException("ClusterProxyConnectionNotExists", id);
    }

    /// <summary>
    ///     Create ClusterProxyConnection
    /// </summary>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public ClusterProxyConnection CreateClusterProxyConnection(string host, int port, string username, string password,
        ProxyType type)
    {
        var clusterProxyConnection = new ClusterProxyConnection
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            Type = type
        };
        _unitOfWork.ClusterProxyConnectionRepository.Insert(clusterProxyConnection);
        _unitOfWork.Save();

        return clusterProxyConnection;
    }

    /// <summary>
    ///     Modify ClusterProxyConnection
    /// </summary>
    /// <param name="id"></param>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public ClusterProxyConnection ModifyClusterProxyConnection(long id, string host, int port, string username,
        string password, ProxyType type)
    {
        var existingClusterProxyConnection = _unitOfWork.ClusterProxyConnectionRepository.GetById(id) ??
                                             throw new RequestedObjectDoesNotExistException(
                                                 "ClusterProxyConnectionNotExists", id);

        existingClusterProxyConnection.Host = host;
        existingClusterProxyConnection.Port = port;
        existingClusterProxyConnection.Username = username;
        existingClusterProxyConnection.Password = password;
        existingClusterProxyConnection.Type = type;
        _unitOfWork.ClusterProxyConnectionRepository.Update(existingClusterProxyConnection);
        _unitOfWork.Save();

        return existingClusterProxyConnection;
    }

    /// <summary>
    ///     Remove ClusterProxyConnection
    /// </summary>
    /// <param name="id"></param>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public void RemoveClusterProxyConnection(long id)
    {
        var existingClusterProxyConnection = _unitOfWork.ClusterProxyConnectionRepository.GetById(id) ??
                                             throw new RequestedObjectDoesNotExistException(
                                                 "ClusterProxyConnectionNotExists", id);

        // remove reference from clusters
        var clusters = _unitOfWork.ClusterRepository.GetAllByClusterProxyConnectionId(id);
        foreach (var c in clusters)
        {
            c.ProxyConnection = null;
            _unitOfWork.ClusterRepository.Update(c);
        }

        // soft delete cluster proxy connection
        existingClusterProxyConnection.IsDeleted = true;
        _unitOfWork.ClusterProxyConnectionRepository.Update(existingClusterProxyConnection);
        _unitOfWork.Save();
    }

    /// <summary>
    ///     Get FileTransferMethod by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public FileTransferMethod GetFileTransferMethodById(long id)
    {
        var fileTransferMethod = _unitOfWork.FileTransferMethodRepository.GetById(id);

        return fileTransferMethod ?? throw new RequestedObjectDoesNotExistException("FileTransferMethodNotFound", id);
    }

    /// <summary>
    ///     Create FileTransferMethod
    /// </summary>
    /// <param name="serverHostname"></param>
    /// <param name="protocol"></param>
    /// <param name="clusterId"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public FileTransferMethod CreateFileTransferMethod(string serverHostname, FileTransferProtocol protocol,
        long clusterId, int? port)
    {
        _ = _unitOfWork.ClusterRepository.GetById(clusterId) ??
            throw new RequestedObjectDoesNotExistException("ClusterNotFound", clusterId);

        var fileTransferMethod = new FileTransferMethod
        {
            ServerHostname = serverHostname,
            Protocol = protocol,
            ClusterId = clusterId,
            Port = port
        };
        _unitOfWork.FileTransferMethodRepository.Insert(fileTransferMethod);
        _unitOfWork.Save();

        return fileTransferMethod;
    }

    /// <summary>
    ///     Modify FileTransferMethod
    /// </summary>
    /// <param name="id"></param>
    /// <param name="serverHostname"></param>
    /// <param name="protocol"></param>
    /// <param name="clusterId"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public FileTransferMethod ModifyFileTransferMethod(long id, string serverHostname, FileTransferProtocol protocol,
        long clusterId, int? port)
    {
        var existingFileTransferMethod = _unitOfWork.FileTransferMethodRepository.GetById(id) ??
                                         throw new RequestedObjectDoesNotExistException("FileTransferMethodNotFound",
                                             id);

        _ = _unitOfWork.ClusterRepository.GetById(clusterId) ??
            throw new RequestedObjectDoesNotExistException("ClusterNotFound", clusterId);

        existingFileTransferMethod.ServerHostname = serverHostname;
        existingFileTransferMethod.Protocol = protocol;
        existingFileTransferMethod.ClusterId = clusterId;
        existingFileTransferMethod.Port = port;
        _unitOfWork.FileTransferMethodRepository.Update(existingFileTransferMethod);
        _unitOfWork.Save();

        return existingFileTransferMethod;
    }

    /// <summary>
    ///     Remove FileTransferMethod
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sessionCode"></param>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public void RemoveFileTransferMethod(long id)
    {
        var existingFileTransferMethod = _unitOfWork.FileTransferMethodRepository.GetById(id)
                                         ?? throw new RequestedObjectDoesNotExistException("FileTransferMethodNotFound",
                                             id);

        // remove reference from node types
        var nodeTypes = _unitOfWork.ClusterNodeTypeRepository.GetAllByFileTransferMethod(id);
        foreach (var nt in nodeTypes)
        {
            nt.FileTransferMethod = null;
            _unitOfWork.ClusterNodeTypeRepository.Update(nt);
        }

        // remove reference from job specification
        var jobSpecifications = _unitOfWork.JobSpecificationRepository.GetAllByFileTransferMethod(id);
        foreach (var js in jobSpecifications)
        {
            js.FileTransferMethod = null;
            _unitOfWork.JobSpecificationRepository.Update(js);
        }

        // soft delete file transfer method
        existingFileTransferMethod.IsDeleted = true;
        _unitOfWork.FileTransferMethodRepository.Update(existingFileTransferMethod);
        _unitOfWork.Save();
    }

    /// <summary>
    ///     Get ClusterNodeTypeAggregation by id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public ClusterNodeTypeAggregation GetClusterNodeTypeAggregationById(long id)
    {
        var clusterNodeTypeAggregation = _unitOfWork.ClusterNodeTypeAggregationRepository.GetById(id);

        return clusterNodeTypeAggregation ??
               throw new RequestedObjectDoesNotExistException("ClusterNodeTypeAggregationNotFound", id);
    }

    /// <summary>
    ///     Get all ClusterNodeTypeAggregation
    /// </summary>
    /// <returns></returns>
    public List<ClusterNodeTypeAggregation> GetClusterNodeTypeAggregations()
    {
        return _unitOfWork.ClusterNodeTypeAggregationRepository.GetAll().ToList();
    }

    /// <summary>
    ///     Create ClusterNodeTypeAggregation
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="allocationType"></param>
    /// <param name="validityFrom"></param>
    /// <param name="validityTo"></param>
    /// <returns></returns>
    public ClusterNodeTypeAggregation CreateClusterNodeTypeAggregation(string name, string description,
        string allocationType, DateTime validityFrom, DateTime? validityTo)
    {
        var clusterNodeTypeAggregation = new ClusterNodeTypeAggregation
        {
            Name = name,
            Description = description,
            AllocationType = allocationType,
            ValidityFrom = validityFrom,
            ValidityTo = validityTo
        };
        _unitOfWork.ClusterNodeTypeAggregationRepository.Insert(clusterNodeTypeAggregation);
        _unitOfWork.Save();

        return clusterNodeTypeAggregation;
    }

    /// <summary>
    ///     Modify ClusterNodeTypeAggregation
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="allocationType"></param>
    /// <param name="validityFrom"></param>
    /// <param name="validityTo"></param>
    /// <returns></returns>
    public ClusterNodeTypeAggregation ModifyClusterNodeTypeAggregation(long id, string name, string description,
        string allocationType, DateTime validityFrom, DateTime? validityTo)
    {
        var clusterNodeTypeAggregation = _unitOfWork.ClusterNodeTypeAggregationRepository.GetById(id) ??
                                         throw new RequestedObjectDoesNotExistException(
                                             "ClusterNodeTypeAggregationNotFound", id);

        clusterNodeTypeAggregation.Name = name;
        clusterNodeTypeAggregation.Description = description;
        clusterNodeTypeAggregation.AllocationType = allocationType;
        clusterNodeTypeAggregation.ValidityFrom = validityFrom;
        clusterNodeTypeAggregation.ValidityTo = validityTo;
        _unitOfWork.ClusterNodeTypeAggregationRepository.Update(clusterNodeTypeAggregation);
        _unitOfWork.Save();

        return clusterNodeTypeAggregation;
    }

    /// <summary>
    ///     Remove ClusterNodeTypeAggregation
    /// </summary>
    /// <param name="id"></param>
    public void RemoveClusterNodeTypeAggregation(long id)
    {
        var clusterNodeTypeAggregation = _unitOfWork.ClusterNodeTypeAggregationRepository.GetById(id) ??
                                         throw new RequestedObjectDoesNotExistException(
                                             "ClusterNodeTypeAggregationNotFound", id);

        // remove ClusterNodeTypeAggregationAccounting
        clusterNodeTypeAggregation.ClusterNodeTypeAggregationAccountings.ForEach(agg => { agg.IsDeleted = true; });
        // remove reference from node types
        clusterNodeTypeAggregation.ClusterNodeTypes.ForEach(nt => { nt.ClusterNodeTypeAggregation = null; });
        // remove ProjectClusterNodeTypeAggregation
        clusterNodeTypeAggregation.ProjectClusterNodeTypeAggregations.ForEach(p =>
        {
            p.ModifiedAt = DateTime.Now;
            p.IsDeleted = true;
        });
        // soft delete cluster node type aggregation
        clusterNodeTypeAggregation.IsDeleted = true;
        _unitOfWork.ClusterNodeTypeAggregationRepository.Update(clusterNodeTypeAggregation);
        _unitOfWork.Save();
    }

    /// <summary>
    ///     Get ClusterNodeTypeAggregationAccounting by clusterNodeTypeAggregationId and accountingId
    /// </summary>
    /// <param name="clusterNodeTypeAggregationId"></param>
    /// <param name="accountingId"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public ClusterNodeTypeAggregationAccounting GetClusterNodeTypeAggregationAccountingById(
        long clusterNodeTypeAggregationId, long accountingId)
    {
        return _unitOfWork.ClusterNodeTypeAggregationAccountingRepository.GetById(clusterNodeTypeAggregationId,
                   accountingId)
               ?? throw new RequestedObjectDoesNotExistException("ClusterNodeTypeAggregationAccountingNotFound",
                   clusterNodeTypeAggregationId, accountingId);
    }

    /// <summary>
    ///     Create ClusterNodeTypeAggregationAccounting
    /// </summary>
    /// <param name="clusterNodeTypeAggregationId"></param>
    /// <param name="accountingId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidRequestException"></exception>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public ClusterNodeTypeAggregationAccounting CreateClusterNodeTypeAggregationAccounting(
        long clusterNodeTypeAggregationId, long accountingId)
    {
        // we want to check if there is soft deleted entity with same ids because M:N connection entities cause exceptions if we want to create with same ids
        // so we restore soft deleted instead of creating new one
        var clusterNodeTypeAggregationAccounting =
            _unitOfWork.ClusterNodeTypeAggregationAccountingRepository.GetByIdIncludeSoftDeleted(
                clusterNodeTypeAggregationId, accountingId);

        if (clusterNodeTypeAggregationAccounting != null && !clusterNodeTypeAggregationAccounting.IsDeleted)
            throw new InvalidRequestException("ClusterNodeTypeAggregationAccountingAlreadyExists",
                clusterNodeTypeAggregationId, accountingId);

        if (_unitOfWork.ClusterNodeTypeAggregationRepository.GetById(clusterNodeTypeAggregationId) == null)
            throw new RequestedObjectDoesNotExistException("ClusterNodeTypeAggregationNotFound",
                clusterNodeTypeAggregationId);

        if (_unitOfWork.AccountingRepository.GetById(accountingId) == null)
            throw new RequestedObjectDoesNotExistException("AccountingNotFound", accountingId);

        // restore soft deleted entity - otherwise create new
        if (clusterNodeTypeAggregationAccounting != null && clusterNodeTypeAggregationAccounting.IsDeleted)
        {
            clusterNodeTypeAggregationAccounting.IsDeleted = false;
        }
        else
        {
            clusterNodeTypeAggregationAccounting = new ClusterNodeTypeAggregationAccounting
            {
                ClusterNodeTypeAggregationId = clusterNodeTypeAggregationId,
                AccountingId = accountingId
            };
            _unitOfWork.ClusterNodeTypeAggregationAccountingRepository.Insert(clusterNodeTypeAggregationAccounting);
        }

        _unitOfWork.Save();

        return clusterNodeTypeAggregationAccounting;
    }

    /// <summary>
    ///     Remove ClusterNodeTypeAggregationAccounting
    /// </summary>
    /// <param name="clusterNodeTypeAggregationId"></param>
    /// <param name="accountingId"></param>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public void RemoveClusterNodeTypeAggregationAccounting(long clusterNodeTypeAggregationId, long accountingId)
    {
        var clusterNodeTypeAggregationAccounting =
            _unitOfWork.ClusterNodeTypeAggregationAccountingRepository.GetById(clusterNodeTypeAggregationId,
                accountingId)
            ?? throw new RequestedObjectDoesNotExistException("ClusterNodeTypeAggregationAccountingNotFound",
                clusterNodeTypeAggregationId, accountingId);

        // soft delete cluster node type aggregation accounting
        clusterNodeTypeAggregationAccounting.IsDeleted = true;
        _unitOfWork.ClusterNodeTypeAggregationAccountingRepository.Update(clusterNodeTypeAggregationAccounting);
        _unitOfWork.Save();
    }

    /// <summary>
    ///     Get Accounting by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public Accounting GetAccountingById(long id)
    {
        return _unitOfWork.AccountingRepository.GetById(id)
               ?? throw new RequestedObjectDoesNotExistException("AccountingNotFound", id);
    }

    /// <summary>
    ///     Create Accounting
    /// </summary>
    /// <param name="formula"></param>
    /// <param name="validityFrom"></param>
    /// <param name="validityTo"></param>
    /// <returns></returns>
    public Accounting CreateAccounting(string formula, DateTime validityFrom, DateTime? validityTo)
    {
        var accounting = new Accounting
        {
            Formula = formula,
            ValidityFrom = validityFrom,
            ValidityTo = validityTo,
            CreatedAt = DateTime.UtcNow
        };
        _unitOfWork.AccountingRepository.Insert(accounting);
        _unitOfWork.Save();

        return accounting;
    }

    /// <summary>
    ///     Modify Accounting
    /// </summary>
    /// <param name="id"></param>
    /// <param name="formula"></param>
    /// <param name="validityFrom"></param>
    /// <param name="validityTo"></param>
    /// <returns></returns>
    public Accounting ModifyAccounting(long id, string formula, DateTime validityFrom, DateTime? validityTo)
    {
        var accounting = _unitOfWork.AccountingRepository.GetById(id)
                         ?? throw new RequestedObjectDoesNotExistException("AccountingNotFound", id);

        accounting.Formula = formula;
        accounting.ValidityFrom = validityFrom;
        accounting.ValidityTo = validityTo;
        accounting.ModifiedAt = DateTime.UtcNow;
        _unitOfWork.AccountingRepository.Update(accounting);
        _unitOfWork.Save();

        return accounting;
    }

    /// <summary>
    ///     Remove Accounting
    /// </summary>
    /// <param name="id"></param>
    public void RemoveAccounting(long id)
    {
        var accounting = _unitOfWork.AccountingRepository.GetById(id)
                         ?? throw new RequestedObjectDoesNotExistException("AccountingNotFound", id);

        // remove ClusterNodeTypeAggregationAccountings
        accounting.ClusterNodeTypeAggregationAccountings.ForEach(agg => { agg.IsDeleted = true; });
        // soft delete accounting
        accounting.ModifiedAt = DateTime.UtcNow;
        accounting.IsDeleted = true;
        _unitOfWork.AccountingRepository.Update(accounting);
        _unitOfWork.Save();
    }

    /// <summary>
    ///     Get ProjectClusterNodeTypeAggregation by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public ProjectClusterNodeTypeAggregation GetProjectClusterNodeTypeAggregationById(long projectId,
        long clusterNodeTypeAggregationId)
    {
        return _unitOfWork.ProjectClusterNodeTypeAggregationRepository.GetById(projectId, clusterNodeTypeAggregationId)
               ?? throw new RequestedObjectDoesNotExistException("ProjectClusterNodeTypeAggregationNotFound", projectId,
                   clusterNodeTypeAggregationId);
    }

    /// <summary>
    ///     Get ProjectClusterNodeTypeAggregation by ProjectId
    /// </summary>
    /// <param name="projectId"></param>
    /// <returns></returns>
    public List<ProjectClusterNodeTypeAggregation> GetProjectClusterNodeTypeAggregationsByProjectId(long projectId)
    {
        return _unitOfWork.ProjectClusterNodeTypeAggregationRepository.GetAllByProjectId(projectId);
    }

    /// <summary>
    ///     Create ProjectClusterNodeTypeAggregation
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="clusterNodeTypeAggregationId"></param>
    /// <param name="allocationAmount"></param>
    /// <returns></returns>
    public ProjectClusterNodeTypeAggregation CreateProjectClusterNodeTypeAggregation(long projectId,
        long clusterNodeTypeAggregationId, long allocationAmount)
    {
        // we want to check if there is soft deleted entity with same ids because M:N connection entities cause exceptions if we want to create with same ids
        // so we restore soft deleted instead of creating new one
        var projectClusterNodeTypeAggregation =
            _unitOfWork.ProjectClusterNodeTypeAggregationRepository.GetByIdIncludeSoftDeleted(projectId,
                clusterNodeTypeAggregationId);

        if (projectClusterNodeTypeAggregation != null && !projectClusterNodeTypeAggregation.IsDeleted)
            throw new InvalidRequestException("ProjectClusterNodeTypeAggregationAlreadyExists", projectId,
                clusterNodeTypeAggregationId);

        if (_unitOfWork.ClusterNodeTypeAggregationRepository.GetById(clusterNodeTypeAggregationId) == null)
            throw new RequestedObjectDoesNotExistException("ClusterNodeTypeAggregationNotFound",
                clusterNodeTypeAggregationId);

        if (_unitOfWork.ProjectRepository.GetById(projectId) == null)
            throw new RequestedObjectDoesNotExistException("ProjectNotFound");

        // restore soft deleted entity - otherwise create new
        if (projectClusterNodeTypeAggregation != null && projectClusterNodeTypeAggregation.IsDeleted)
        {
            projectClusterNodeTypeAggregation.IsDeleted = false;
            projectClusterNodeTypeAggregation.AllocationAmount = allocationAmount;
            projectClusterNodeTypeAggregation.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
            projectClusterNodeTypeAggregation = new ProjectClusterNodeTypeAggregation
            {
                ProjectId = projectId,
                ClusterNodeTypeAggregationId = clusterNodeTypeAggregationId,
                AllocationAmount = allocationAmount
            };
            _unitOfWork.ProjectClusterNodeTypeAggregationRepository.Insert(projectClusterNodeTypeAggregation);
        }

        _unitOfWork.Save();

        return projectClusterNodeTypeAggregation;
    }

    /// <summary>
    ///     Create ProjectClusterNodeTypeAggregation
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="clusterNodeTypeAggregationId"></param>
    /// <param name="allocationAmount"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    public ProjectClusterNodeTypeAggregation ModifyProjectClusterNodeTypeAggregation(long projectId,
        long clusterNodeTypeAggregationId, long allocationAmount)
    {
        var projectClusterNodeTypeAggregation =
            _unitOfWork.ProjectClusterNodeTypeAggregationRepository.GetById(projectId, clusterNodeTypeAggregationId)
            ?? throw new RequestedObjectDoesNotExistException("ProjectClusterNodeTypeAggregationNotFound", projectId,
                clusterNodeTypeAggregationId);

        projectClusterNodeTypeAggregation.AllocationAmount = allocationAmount;
        projectClusterNodeTypeAggregation.ModifiedAt = DateTime.UtcNow;
        _unitOfWork.ProjectClusterNodeTypeAggregationRepository.Update(projectClusterNodeTypeAggregation);
        _unitOfWork.Save();

        return projectClusterNodeTypeAggregation;
    }

    /// <summary>
    ///     Remove ProjectClusterNodeTypeAggregation
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="clusterNodeTypeAggregationId"></param>
    /// <exception cref="RequestedObjectDoesNotExistException"></exception>
    public void RemoveProjectClusterNodeTypeAggregation(long projectId, long clusterNodeTypeAggregationId)
    {
        var projectClusterNodeTypeAggregation =
            _unitOfWork.ProjectClusterNodeTypeAggregationRepository.GetById(projectId, clusterNodeTypeAggregationId)
            ?? throw new RequestedObjectDoesNotExistException("ProjectClusterNodeTypeAggregationNotFound", projectId,
                clusterNodeTypeAggregationId);

        // soft delete project cluster node type aggregation
        projectClusterNodeTypeAggregation.ModifiedAt = DateTime.UtcNow;
        projectClusterNodeTypeAggregation.IsDeleted = true;
        _unitOfWork.ProjectClusterNodeTypeAggregationRepository.Update(projectClusterNodeTypeAggregation);
        _unitOfWork.Save();
    }

    private SecureShellKey CreateSecureShellKey(string username, string password, Project project, long? adaptorUserId)
    {
        _logger.Info($"Creating SSH key for user {username} for project {project.Name}.");
        var clusterProjects = _unitOfWork.ClusterProjectRepository.GetAll().Where(x => x.ProjectId == project.Id)
            .ToList();
        if (!clusterProjects.Any()) throw new InputValidationException("ProjectNoAssignToCluster");

        SSHGenerator sshGenerator = new();
        var passphrase = StringUtils.GetRandomString();
        var secureShellKey = sshGenerator.GetEncryptedSecureShellKey(username, passphrase);

        var serviceCredentials = CreateClusterAuthenticationCredentials(username, password, secureShellKey, passphrase,
            clusterProjects.FirstOrDefault()?.Cluster);
        var nonServiceCredentials = CreateClusterAuthenticationCredentials(username, password, secureShellKey,
            passphrase, clusterProjects.FirstOrDefault()?.Cluster);

        foreach (var clusterProject in clusterProjects)
        {
            var serviceAccount =
                _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                    clusterProject.ClusterId, project.Id, adaptorUserId: adaptorUserId);

            if (serviceAccount == null)
            {
                serviceCredentials.ClusterProjectCredentials.Add(
                    CreateClusterProjectCredentials(clusterProject, serviceCredentials, true, adaptorUserId));
                _logger.Info(
                    $"Service account not found or deleted. Creating new service account for project {project.Id} on cluster {clusterProject.ClusterId}.");
            }

            nonServiceCredentials.ClusterProjectCredentials.Add(
                CreateClusterProjectCredentials(clusterProject, nonServiceCredentials, false, adaptorUserId));
            _logger.Info($"Creating new SSH key for project {project.Id} on cluster {clusterProject.ClusterId}.");
        }

        project.ModifiedAt = DateTime.UtcNow;
        _unitOfWork.ProjectRepository.Update(project);
        var serviceCredentialStored = false;
        if (serviceCredentials.ClusterProjectCredentials.Any())
        {
            _unitOfWork.ClusterAuthenticationCredentialsRepository.Insert(serviceCredentials);
            serviceCredentialStored = true;
        }

        _unitOfWork.ClusterAuthenticationCredentialsRepository.Insert(nonServiceCredentials);
        _unitOfWork.Save();

        var vaultConnector = new VaultConnector();
        bool vaultSuccess;

        if (serviceCredentialStored)
        {
            vaultSuccess = vaultConnector.SetClusterAuthenticationCredentials(serviceCredentials.ExportVaultData());
            
            if (!vaultSuccess)
            {
                _logger.Warn("Failed to set service credentials in the vault. Rolling back database insert.");
                // Perform rollback for serviceCredentials insertion here if needed
                _unitOfWork.ClusterAuthenticationCredentialsRepository.Delete(nonServiceCredentials);
                _unitOfWork.ClusterAuthenticationCredentialsRepository.Delete(serviceCredentials);
                _unitOfWork.Save();
                throw new SecureVaultException("ConnectionFailed");
            }
        }

        vaultSuccess = vaultConnector.SetClusterAuthenticationCredentials(nonServiceCredentials.ExportVaultData());

        if (!vaultSuccess)
        {
            _logger.Warn("Failed to set non-service credentials in the vault. Rolling back database insert.");
            // Perform rollback for nonServiceCredentials insertion here
            _unitOfWork.ClusterAuthenticationCredentialsRepository.Delete(nonServiceCredentials);
            _unitOfWork.Save();
            throw new SecureVaultException("ConnectionFailed");
        }

        return secureShellKey;
    }


    #region SubProject

    /// <summary>
    ///     Creates a new subproject if it does not exist
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public SubProject CreateSubProject(string identifier, long projectId)
    {
        var project = _unitOfWork.ProjectRepository.GetById(projectId)
                      ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound");

        var subProject = _unitOfWork.SubProjectRepository.GetByIdentifier(identifier, projectId);
        if (subProject is not null &&
            (subProject.EndDate <= DateTime.UtcNow || subProject.StartDate >= DateTime.UtcNow))
            throw new InputValidationException("SubProjectDeletedOrEnded");

        if (subProject is not null)
        {
            //already exists, reuse it
            return subProject;
        }

        //create new 
        SubProject newSubProject = new()
        {
            Identifier = identifier,
            CreatedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow,
            EndDate = project.EndDate,
            IsDeleted = false,
            ProjectId = projectId
        };
        _unitOfWork.SubProjectRepository.Insert(newSubProject);
        _unitOfWork.Save();
        return newSubProject;
    }

    public SubProject CreateSubProject(long modelProjectId, string modelIdentifier, string modelDescription,
        DateTime modelStartDate, DateTime? modelEndDate)
    {
        var project = _unitOfWork.ProjectRepository.GetById(modelProjectId)
                      ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound");

        if (modelEndDate.HasValue && modelEndDate.Value > project.EndDate)
            throw new InputValidationException("SubProjectEndDateAfterProjectEndDate");
        if (modelStartDate < project.StartDate)
            throw new InputValidationException("SubProjectStartDateBeforeProjectStartDate");

        //test if not exist subproject with the same identifier
        if (_unitOfWork.SubProjectRepository.GetByIdentifier(modelIdentifier, modelProjectId) != null)
            throw new InputValidationException("SubProjectIdentifierAlreadyExists");
        SubProject newSubProject = new()
        {
            Identifier = modelIdentifier,
            Description = modelDescription,
            CreatedAt = DateTime.UtcNow,
            StartDate = modelStartDate,
            EndDate = modelEndDate,
            IsDeleted = false,
            ProjectId = modelProjectId
        };
        _unitOfWork.SubProjectRepository.Insert(newSubProject);
        _unitOfWork.Save();
        return newSubProject;
    }

    public SubProject ModifySubProject(long modelId, string modelIdentifier, string modelDescription,
        DateTime modelStartDate,
        DateTime? modelEndDate)
    {
        var subProject = _unitOfWork.SubProjectRepository.GetById(modelId)
                         ?? throw new RequestedObjectDoesNotExistException("SubProjectNotFound");

        var project = _unitOfWork.ProjectRepository.GetById(subProject.ProjectId)
                      ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound");

        if (modelEndDate.HasValue && modelEndDate.Value > project.EndDate)
            throw new InputValidationException("SubProjectEndDateAfterProjectEndDate");
        if (modelStartDate < project.StartDate)
            throw new InputValidationException("SubProjectStartDateBeforeProjectStartDate");


        var subProjectWithSameIdentifier =
            _unitOfWork.SubProjectRepository.GetByIdentifier(modelIdentifier, subProject.ProjectId);
        if (subProjectWithSameIdentifier != null && subProjectWithSameIdentifier.Id != modelId)
            throw new InputValidationException("SubProjectIdentifierAlreadyExists");
        subProject.Identifier = modelIdentifier;
        subProject.Description = modelDescription;
        subProject.StartDate = modelStartDate;
        subProject.EndDate = modelEndDate;
        subProject.ModifiedAt = DateTime.UtcNow;
        _unitOfWork.SubProjectRepository.Update(subProject);
        _unitOfWork.Save();
        return subProject;
    }

    public void RemoveSubProject(long modelId)
    {
        var subProject = _unitOfWork.SubProjectRepository.GetById(modelId)
                         ?? throw new RequestedObjectDoesNotExistException("SubProjectNotFound");

        subProject.IsDeleted = true;
        subProject.ModifiedAt = DateTime.UtcNow;
        _unitOfWork.SubProjectRepository.Update(subProject);
        _unitOfWork.Save();
    }

    public void ComputeAccounting(DateTime modelStartTime, DateTime modelEndTime, long projectId)
    {
        //get all submittedtasks from project and compute with formula
        var project = _unitOfWork.ProjectRepository.GetById(projectId) ??
                      throw new RequestedObjectDoesNotExistException("ProjectNotFound");

        var submittedTasks = _unitOfWork.SubmittedTaskInfoRepository
            .GetAll()
            .Where(t => t.StartTime >= modelStartTime
                        && t.EndTime <= modelEndTime
                        && t.Project.Id == projectId)
            .ToList();

        var accountingState = new AccountingState
        {
            ProjectId = project.Id,
            Project = project,
            AccountingStateType = AccountingStateType.Running,
            ComputingStartDate = DateTime.UtcNow,
            TriggeredAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        project.AccountingStates.Add(accountingState);
        _unitOfWork.ProjectRepository.Update(project);
        _logger.Info(
            $"Accounting for project {project.Id} has been started. Total tasks to compute: {submittedTasks.Count}.");
        //compute accounting
        foreach (var submittedTask in submittedTasks)
        {
            //parse all parameters to dictionary
            var parsedParameters = submittedTask.AllParameters
                .Split(' ')
                .Select(x => x.Split('='))
                .ToDictionary(x => x[0], x => x.Length >= 2 ? x[1] : string.Empty);

            ResourceAccountingUtils.ComputeAccounting(submittedTask, submittedTask, _logger);

            _unitOfWork.SubmittedTaskInfoRepository.Update(submittedTask);
        }

        accountingState.AccountingStateType = AccountingStateType.Finished;
        accountingState.ComputingEndDate = DateTime.UtcNow;
        accountingState.LastUpdatedAt = DateTime.UtcNow;
        _unitOfWork.ProjectRepository.Update(project);
        _unitOfWork.Save();
        _logger.Info($"Accounting for project {project.Id} has been finished.");
    }

    public List<AccountingState> ListAccountingStates(long projectId)
    {
        var project = _unitOfWork.ProjectRepository.GetById(projectId)
                      ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound");

        return project.AccountingStates.ToList();
    }

    #endregion

    #region Private methods

    private void AddCommandTemplateParameterToCommandTemplate(CommandTemplate commandTemplate,
        CommandTemplateParameter commandTemplateParameter)
    {
        //if commandTemplate.CommandParameters does not contain commandTemplateParameter.Identifier
        if (!commandTemplate.CommandParameters.Contains(commandTemplateParameter.Identifier))
            commandTemplate.CommandParameters = string.Join(' ',
                commandTemplate.TemplateParameters.Select(x => $"%%{"{"}{x.Identifier}{"}"}"));
    }

    private void ModifyCommandTemplateParameterFromCommandTemplate(CommandTemplate commandTemplate,
        CommandTemplateParameter commandTemplateParameter, string previousIdentifier)
    {
        // modify commandTemplate.CommandParameters, replace %%{previousIdentifier} with %%{commandTemplateParameter.Identifier}
        commandTemplate.CommandParameters = commandTemplate.CommandParameters.Replace(
            $"%%{"{"}{previousIdentifier}{"}"}", $"%%{"{"}{commandTemplateParameter.Identifier}{"}"}");
    }

    private void RemoveCommandTemplateParameterFromCommandTemplate(CommandTemplate commandTemplate,
        CommandTemplateParameter commandTemplateParameter)
    {
        commandTemplate.TemplateParameters.Remove(commandTemplateParameter);
        commandTemplate.CommandParameters = string.Join(' ',
            commandTemplate.TemplateParameters.Select(x => $"%%{"{"}{x.Identifier}{"}"}"));
    }

    /// <summary>
    ///     Returns the path to the private key file for the specified project
    /// </summary>
    /// <param name="accountingString"></param>
    /// <returns></returns>
    private string GetUniquePrivateKeyPath(string accountingString)
    {
        var directoryPath = Path.Combine(CertificateGeneratorConfiguration.GeneratedKeysDirectory, accountingString);
        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        //get count of files in directory and increment by 1
        var nextId = Directory.GetFiles(directoryPath).Length + 1;
        var keyPath = Path.Combine(directoryPath,
            $"{CertificateGeneratorConfiguration.GeneratedKeyPrefix}_{nextId:D2}");
        return keyPath;
    }

    /// <summary>
    ///     Initializes a new project with the specified parameters
    /// </summary>
    /// <param name="accountingString"></param>
    /// <param name="usageType"></param>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <param name="useAccountingStringForScheduler"></param>
    /// <param name="contact"></param>
    /// <returns></returns>
    private static Project InitializeProject(string accountingString, UsageType usageType, string name,
        string description, DateTime startDate, DateTime endDate, bool useAccountingStringForScheduler, Contact contact, bool isOneToOneMapping)
    {
        return new Project
        {
            AccountingString = accountingString,
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            StartDate = startDate,
            EndDate = endDate,
            UsageType = usageType,
            IsDeleted = false,
            UseAccountingStringForScheduler = useAccountingStringForScheduler,
            ProjectContacts = new List<ProjectContact>
            {
                new()
                {
                    IsPI = true,
                    Contact = contact
                }
            },
            IsOneToOneMapping = isOneToOneMapping
        };
    }

    /// <summary>
    ///     Creates a new user group with the specified parameters
    /// </summary>
    /// <param name="project"></param>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="prefix"></param>
    /// "
    /// <returns></returns>
    private static AdaptorUserGroup CreateAdaptorUserGroup(Project project, string name, string description,
        string prefix)
    {
        return new AdaptorUserGroup
        {
            Name = string.Concat(prefix, name),
            AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>(),
            Description = string.Concat(prefix, description),
            Project = project
        };
    }

    /// <summary>
    ///     Create auth credentials
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="sshKey"></param>
    /// <param name="passphrase"></param>
    /// <param name="publicKeyFingerprint"></param>
    /// <param name="cluster"></param>
    /// <returns></returns>
    private static ClusterAuthenticationCredentials CreateClusterAuthenticationCredentials(string username,
        string password, SecureShellKey sshKey, string passphrase, Cluster cluster)
    {
        ClusterAuthenticationCredentials credentials = new()
        {
            Username = username,
            Password = password,
            PrivateKey = sshKey.PrivateKeyPEM,
            PrivateKeyPassphrase = passphrase,
            CipherType = CipherGeneratorConfiguration.Type,
            PublicKeyFingerprint = sshKey.PublicKeyFingerprint,
            ClusterProjectCredentials = new List<ClusterProjectCredential>(),
            IsGenerated = true
        };
        credentials.AuthenticationType =
            ClusterAuthenticationCredentialsUtils.GetCredentialsAuthenticationType(credentials, cluster);
        return credentials;
    }

    /// <summary>
    ///     Creates a new cluster reference to project and map credentials with the specified parameters
    /// </summary>
    /// <param name="clusterProject"></param>
    /// <param name="clusterAuthenticationCredentials"></param>
    /// <param name="isServiceAccount"></param>
    /// <returns></returns>
    private static ClusterProjectCredential CreateClusterProjectCredentials(ClusterProject clusterProject,
        ClusterAuthenticationCredentials clusterAuthenticationCredentials, bool isServiceAccount, long? adaptorUserId = null)
    {
        return new ClusterProjectCredential
        {
            ClusterProject = clusterProject,
            ClusterAuthenticationCredentials = clusterAuthenticationCredentials,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
            IsServiceAccount = isServiceAccount,
            AdaptorUserId = adaptorUserId
        };
    }

    /// <summary>
    ///     Computes the fingerprint of the specified public key in base64 format
    /// </summary>
    /// <param name="publicKey"></param>
    /// <returns>SHA256 hash</returns>
    /// <exception cref="InputValidationException"></exception>
    private static string ComputePublicKeyFingerprint(string publicKey)
    {
        publicKey = publicKey.Replace("\n", "");
        Regex base64Regex = new(@"([A-Za-z0-9+\/=]+=)");
        var base64Match = base64Regex.Match(publicKey);

        if (!base64Match.Success || !TryFromBase64String(base64Match.Value, out var base64EncodedBytes))
            throw new InputValidationException("InvalidPublicKey");

        var fingerprintBytes = DigestUtilities.CalculateDigest("SHA256", base64EncodedBytes);
        return BitConverter.ToString(fingerprintBytes).Replace("-", string.Empty).ToLower();
    }

    private static bool TryFromBase64String(string base64, out byte[] result)
    {
        try
        {
            result = Convert.FromBase64String(base64);
            return true;
        }
        catch (FormatException)
        {
            result = null;
            return false;
        }
    }

    #endregion
}