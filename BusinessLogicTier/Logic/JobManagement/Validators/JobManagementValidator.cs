using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.Exceptions.External;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.Utils.Validation;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement.Validators;

internal class JobManagementValidator : AbstractValidator
{
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="validationObj">Validation Object</param>
    internal JobManagementValidator(object validationObj, IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
        : base(validationObj)
    {
        _unitOfWork = unitOfWork;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
    }

    #endregion

    #region Override Methods

    /// <summary>
    ///     Validation
    /// </summary>
    /// <returns></returns>
    public override ValidationResult Validate()
    {
        var message = _validationObject switch
        {
            JobSpecification jobSpecification => ValidateJobSpecification(jobSpecification),
            _ => string.Empty
        };
        return new ValidationResult(string.IsNullOrEmpty(message), message);
    }

    #endregion

    #region Local Methods

    /// <summary>
    ///     Validate Job specification
    /// </summary>
    /// <param name="job">Job specification</param>
    /// <returns></returns>
    private string ValidateJobSpecification(JobSpecification job)
    {
        ValidateRequestedCluster(job);
        ValidateRequestedProject(job);

        if (job.Id != 0 && _unitOfWork.JobSpecificationRepository.GetById(job.Id) == null)
            _ = _messageBuilder.AppendLine($"Job with Id {job.Id} does not exist in the system");

        if (job.Tasks.Count <= 0) _ = _messageBuilder.AppendLine("Job must have at least one task");

        //Task Dependency
        for (var i = 0; i < job.Tasks.Count; i++)
        {
            //Task Validation
            ValidateTaskSpecification(job.Tasks[i]);

            if (job.Tasks[i].CommandTemplate is null || job.Tasks[i].CommandTemplate.IsDeleted)
                //_messageBuilder.AppendLine($"Command Template does not exist.");
                //this is validated in jobSpec
                break;


            //Validation cluster in tasks
            if (job.Tasks[i].CommandTemplate.ClusterNodeType.Cluster.Id != job.Cluster.Id)
                _ = _messageBuilder.AppendLine($"Task \"{job.Tasks[i].Name}\" must used same HPC Cluster as job " +
                                               $"\"{job.Name}\".");

            if (job.FileTransferMethodId != job.Tasks[i].CommandTemplate.ClusterNodeType.FileTransferMethodId)
                _ = _messageBuilder.AppendLine($"Command template \"{job.Tasks[i].CommandTemplate.Id}\" for task " +
                                               $"\"{job.Tasks[i].Name}\" has different file transfer method " +
                                               $"\"{job.Tasks[i].CommandTemplate.ClusterNodeType.FileTransferMethodId}\" " +
                                               $"than job file transfer method \"{job.FileTransferMethodId}\".");

            if (job.Tasks[i].CommandTemplate.ClusterNodeType.Id != job.Tasks[i].ClusterNodeTypeId)
                _ = _messageBuilder.AppendLine($"Task \"{job.Tasks[i].Name}\" must used same ClusterNodeTypeId " +
                                               $"\"{job.Tasks[i].ClusterNodeTypeId}\" which is defined in CommandTemplate " +
                                               $"(ClusterNodeTypeId=\"{job.Tasks[i].CommandTemplate.ClusterNodeType.Id}\").");


            if (job.Tasks[i].DependsOn != null && job.Tasks[i].DependsOn.Count > 0)
            {
                List<TaskSpecification> prevTasks = new();
                foreach (var dependTask in job.Tasks[i].DependsOn)
                {
                    if (dependTask.TaskSpecification == dependTask.ParentTaskSpecification)
                        //Inself reference
                        _ = _messageBuilder.AppendLine(
                            $"Depending task \"{dependTask.TaskSpecification.Name}\" for task " +
                            $"\"{job.Tasks[i].Name}\" references inself.");

                    var prevTask =
                        prevTasks.FirstOrDefault(w => ReferenceEquals(w, dependTask.ParentTaskSpecification));
                    if (prevTask is null)
                        prevTasks.Add(dependTask.ParentTaskSpecification);
                    else
                        //Same dependency
                        _ = _messageBuilder.AppendLine(
                            $"Depending task \"{dependTask.ParentTaskSpecification.Name}\" for task " +
                            $"\"{job.Tasks[i].Name}\" twice same reference.");

                    var previousTask = false;
                    for (var j = i - 1; j >= 0; j--)
                        if (dependTask.ParentTaskSpecification == job.Tasks[j])
                            previousTask = true;

                    if (!previousTask)
                        //Circular dependency
                        _ = _messageBuilder.AppendLine(
                            $"Depending task \"{dependTask.ParentTaskSpecification.Name}\" for task \"{job.Tasks[i].Name}\" " +
                            $"can reference only on previous task.");
                }
            }
        }

        return _messageBuilder.ToString();
    }

    private void ValidateTaskSpecification(TaskSpecification task)
    {
        if (task.Id != 0 && _unitOfWork.TaskSpecificationRepository.GetById(task.Id) == null)
            _ = _messageBuilder.AppendLine($"Task with Id {task.Id} does not exist in the system");

        ValidateWallTimeLimit(task);

        if (task.CommandTemplate is null || task.CommandTemplate.IsDeleted)
        {
            _ = _messageBuilder.AppendLine("Command Template does not exist.");
            return;
        }

        foreach (var parameter in task.CommandTemplate.TemplateParameters.Where(x => x.IsEnabled))
            if (string.IsNullOrEmpty(parameter.Query) &&
                (task.CommandParameterValues == null ||
                 !task.CommandParameterValues.Any(
                     w => w.TemplateParameter == parameter && w.TemplateParameter.IsEnabled)))
                _ = _messageBuilder.AppendLine(
                    $"Command Template parameter \"{parameter.Identifier}\" does not have a value.");

        if (task.ClusterNodeTypeId != task.CommandTemplate.ClusterNodeTypeId)
            _ = _messageBuilder.AppendLine($"Task {task.Name} has wrong CommandTemplate");

        if (task.CommandTemplate.IsDeleted)
            _ = _messageBuilder.AppendLine(
                $"Task {task.Name} has specified deleted CommandTemplateId \"{task.CommandTemplate.Id}\"");

        if (!task.CommandTemplate.IsEnabled)
            _ = _messageBuilder.AppendLine(
                $"Task {task.Name} has specified disabled CommandTemplateId \"{task.CommandTemplate.Id}\"");

        if (task.CommandTemplate.IsGeneric) ValidateGenericCommandTemplateSetup(task);

        if (task.CommandTemplate.ProjectId.HasValue)
            if (task.CommandTemplate.ProjectId != task.JobSpecification.ProjectId)
                _ = _messageBuilder.AppendLine(
                    $"Task {task.Name} has specified CommandTemplateId \"{task.CommandTemplate.Id}\" which is not referenced to ProjectId \"{task.JobSpecification.ProjectId}\" at JobSpecification");
    }

    private void ValidateWallTimeLimit(TaskSpecification task)
    {
        var clusterNodeType = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys)
            .GetClusterNodeTypeById(task.ClusterNodeTypeId);
        if (clusterNodeType == null)
        {
            _ = _messageBuilder.AppendLine(
                $"Requested ClusterNodeType with Id {task.ClusterNodeTypeId} does not exist in the system");
            return;
        }

        if (task.WalltimeLimit.HasValue && task.WalltimeLimit.Value > clusterNodeType.MaxWalltime)
            _ = _messageBuilder.AppendLine(
                $"Defined task {task.Name} has set higher WalltimeLimit ({task.WalltimeLimit.Value}) than the maximum on this cluster node, " +
                $"maximal WallTimeLimit is {clusterNodeType.MaxWalltime}");
    }

    private void ValidateRequestedProject(JobSpecification job)
    {
        var clusterProject =
            _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(job.ClusterId, job.ProjectId);
        if (clusterProject == null)
            _ = _messageBuilder.AppendLine(
                $"Requested project with Id {job.ProjectId} has no reference to cluster with Id {job.ClusterId}.");
    }

    private void ValidateRequestedCluster(JobSpecification job)
    {
        var clusterNodeType = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys)
            .GetClusterById(job.ClusterId);

        if (clusterNodeType == null)
            _ = _messageBuilder.AppendLine($"Requested Cluster with Id {job.ClusterId} does not exist in the system");

        if (job.FileTransferMethod?.ClusterId != job.ClusterId)
            _ = _messageBuilder.AppendLine($"Job {job.Name} has wrong FileTransferMethod");
    }

    private void ValidateGenericCommandTemplateSetup(TaskSpecification task)
    {
        Dictionary<string, string> genericCommandParametres = new();
        //Regex.Matches(task.CommandTemplate.CommandParameters, @"%%\{([\w\.]+)\}", RegexOptions.Compiled)
        foreach (var commandParameterValue in task.CommandParameterValues)
        {
            var key = commandParameterValue.CommandParameterIdentifier;
            var value = commandParameterValue.Value;
            if (!string.IsNullOrEmpty(value)) genericCommandParametres.Add(key, value);
        }

        var scriptPathParameterName = Regex.Match(task.CommandTemplate.CommandParameters, @"%%\{([\w\.]+)\}",
            RegexOptions.Compiled);

        if (!scriptPathParameterName.Success) _ = _messageBuilder.AppendLine("CommandTemplate is wrong");
        var clusterPathToUserScript = genericCommandParametres
            .FirstOrDefault(x => x.Key == scriptPathParameterName.Groups[1].Value).Value;
        if (string.IsNullOrWhiteSpace(clusterPathToUserScript))
            _ = _messageBuilder.AppendLine(
                "User script path parameter, for generic command template, does not have a value.");

        var scriptDefinedParametres = GetUserDefinedScriptParametres(task.ClusterNodeType.Cluster,
            clusterPathToUserScript, task.JobSpecification.ProjectId,
            adaptorUserId: task.JobSpecification.Submitter.Id
        );

        foreach (var parameter in scriptDefinedParametres)
            if (!genericCommandParametres.Select(x => x.Value)
                    .Any(x => Regex.IsMatch(x, $"{parameter}=\\\\\".+\\\\\"")))
                _ = _messageBuilder.AppendLine($"Task specification does not contain '{parameter}' parameter.");
    }

    private IEnumerable<string> GetUserDefinedScriptParametres(Cluster cluster, string userScriptPath, long projectId, long? adaptorUserId)
    {
        try
        {
            var project = _unitOfWork.ProjectRepository.GetById(projectId)
                          ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound");
            var serviceAccount =
                _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(cluster.Id,
                    projectId, requireIsInitialized: true, adaptorUserId: adaptorUserId);
            return SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, project, _sshCertificateAuthorityService, adaptorUserId)
                .GetParametersFromGenericUserScript(cluster, serviceAccount, userScriptPath, _httpContextKeys.Context.SshCaToken).ToList();
        }
        catch (Exception)
        {
            _ = _messageBuilder.AppendLine($"Unable to read or locate script at '{userScriptPath}'.");
            return Enumerable.Empty<string>();
        }
    }

    #endregion
}