using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Converts;
using HEAppE.ExtModels.Management.Models;
using HEAppE.RestApi.Configuration;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.Management;
using HEAppE.ServiceTier.Management;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HEAppE.RestApi.Controllers;

[ApiController]
[Route("heappe/[controller]")]
[Produces("application/json")]
public class ManagementController : BaseController<ManagementController>
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="memoryCache">Memory cache provider</param>
    public ManagementController(ILogger<ManagementController> logger, IMemoryCache memoryCache) : base(logger,
        memoryCache)
    {
        _managementService = new ManagementService();
        _userAndManagementService = new UserAndLimitationManagementService(memoryCache);
    }

    #endregion

    #region Private Methods

    private void ClearListAvailableClusterMethodCache(string sessionCode)
    {
         var memoryCacheKey =$"{nameof(ClusterInformationController.ListAvailableClusters)}_{sessionCode}";
        _cacheProvider.RemoveKeyFromCache(_logger, memoryCacheKey, nameof(CreateProjectAssignmentToCluster));
    }

    #endregion

    #region Instances

    private readonly IManagementService _managementService;
    private readonly IUserAndLimitationManagementService _userAndManagementService;

    #endregion

    #region Methods

    #region InstanceInformation

    /// <summary>
    ///     Get HEAppE Information
    /// </summary>
    /// <param name="sessionCode">SessionCode</param>
    /// <returns></returns>
    [HttpGet("InstanceInformation")]
    [RequestSizeLimit(90)]
    [ProducesResponseType(typeof(InstanceInformationExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult InstanceInformation(string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"InstanceInformation\" Parameters: SessionCode: \"{sessionCode}\"");
        var validationResult = new SessionCodeValidator(sessionCode).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _userAndManagementService.ValidateUserPermissions(sessionCode, AdaptorUserRoleType.Administrator);
        List<ExtendedProjectInfoExt> activeProjectsExtendedInfo = new();
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            activeProjectsExtendedInfo = unitOfWork.ProjectRepository.GetAllActiveProjects()
                ?.Select(p => p.ConvertIntToExtendedInfoExt()).ToList();
        }

        return Ok(new InstanceInformationExt
        {
            Name = DeploymentInformationsConfiguration.Name,
            Description = DeploymentInformationsConfiguration.Description,
            Version = DeploymentInformationsConfiguration.Version,
            DeployedIPAddress = DeploymentInformationsConfiguration.DeployedIPAddress,
            Port = DeploymentInformationsConfiguration.Port,
            URL = DeploymentInformationsConfiguration.Host,
            URLPostfix = DeploymentInformationsConfiguration.HostPostfix,
            DeploymentType = DeploymentInformationsConfiguration.DeploymentType.ConvertIntToExt(),
            ResourceAllocationTypes = DeploymentInformationsConfiguration.ResourceAllocationTypes
                ?.Select(s => s.ConvertIntToExt()).ToList(),
            Projects = activeProjectsExtendedInfo
        });
    }

    /// <summary>
    ///     Get HEAppE Version Information
    /// </summary>
    /// <param name="sessionCode">SessionCode</param>
    /// <returns></returns>
    [HttpGet("VersionInformation")]
    [RequestSizeLimit(90)]
    [ProducesResponseType(typeof(VersionInformationExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult VersionInformation(string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"VersionInformation\" Parameters: SessionCode: \"{sessionCode}\"");
        var validationResult = new SessionCodeValidator(sessionCode).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _userAndManagementService.ValidateUserPermissions(sessionCode, AdaptorUserRoleType.Submitter);
        return Ok(new VersionInformationExt
        {
            Name = DeploymentInformationsConfiguration.Name,
            Description = DeploymentInformationsConfiguration.Description,
            Version = DeploymentInformationsConfiguration.Version
        });
    }

    #endregion

    #region CommandTemplate

    /// <summary>
    ///     List Command Template
    /// </summary>
    /// <param name="sessionCode"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="InputValidationException"></exception>
    [HttpGet("CommandTemplate")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(ExtendedCommandTemplateExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ListCommandTemplate(string sessionCode, long id)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"ListCommandTemplate\"");
        var model = new ListCommandTemplateModel
        {
            SessionCode = sessionCode,
            Id = id
        };
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_managementService.ListCommandTemplate(id, sessionCode));
    }

    /// <summary>
    ///     List Command Templates
    /// </summary>
    /// <param name="sessionCode"></param>
    /// <param name="projectId"></param>
    /// <returns></returns>
    /// <exception cref="InputValidationException"></exception>
    [HttpGet("CommandTemplates")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(List<ExtendedCommandTemplateExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ListCommandTemplates(string sessionCode, long projectId)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"ListCommandTemplates\"");
        var listCommandTemplatesModel = new ListCommandTemplatesModel
        {
            SessionCode = sessionCode,
            ProjectId = projectId
        };
        var validationResult = new ManagementValidator(listCommandTemplatesModel).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_managementService.ListCommandTemplates(projectId, sessionCode));
    }

    /// <summary>
    ///     Create Static Command Template
    /// </summary>
    /// <param name="model">CreateCommandTemplateModel</param>
    /// <returns></returns>
    [HttpPost("CommandTemplate")]
    [RequestSizeLimit(1520)]
    [ProducesResponseType(typeof(ExtendedCommandTemplateExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateCommandTemplate(CreateCommandTemplateModel model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"CreateCommandTemplate\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);
        var commandTemplate = _managementService.CreateCommandTemplateModel(model.Name, model.Description,
            model.ExtendedAllocationCommand, model.ExecutableFile, model.PreparationScript, model.ProjectId,
            model.ClusterNodeTypeId, model.SessionCode);
        List<ExtendedCommandTemplateParameterExt> templateParameters = new();
        foreach (var templateParameter in model.TemplateParameters)
        {
            var createdTemplateParameter = _managementService.CreateCommandTemplateParameter(
                templateParameter.Identifier, templateParameter.Query,
                templateParameter.Description, commandTemplate.Id.Value, model.SessionCode);
            templateParameters.Add(createdTemplateParameter);
        }

        commandTemplate.TemplateParameters = templateParameters.ToArray();
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(commandTemplate);
    }

    /// <summary>
    ///     Create Command Template from Generic Command Template
    /// </summary>
    /// <param name="fromGenericModel">CreateCommandTemplateFromGenericModel</param>
    /// <returns></returns>
    [HttpPost("CommandTemplateFromGeneric")]
    [RequestSizeLimit(1520)]
    [ProducesResponseType(typeof(CommandTemplateExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateCommandTemplateFromGeneric(CreateCommandTemplateFromGenericModel fromGenericModel)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"CreateCommandTemplateFromGeneric\"");
        var validationResult = new ManagementValidator(fromGenericModel).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);
        var commandTemplate = _managementService.CreateCommandTemplateFromGeneric(
            fromGenericModel.GenericCommandTemplateId, fromGenericModel.Name, fromGenericModel.ProjectId,
            fromGenericModel.Description, fromGenericModel.ExtendedAllocationCommand, fromGenericModel.ExecutableFile,
            fromGenericModel.PreparationScript, fromGenericModel.SessionCode);
        ClearListAvailableClusterMethodCache(fromGenericModel.SessionCode);
        return Ok(commandTemplate);
    }

    /// <summary>
    ///     Modify Static Command Template
    /// </summary>
    /// <param name="model">ModifyCommandTemplateModel</param>
    /// <returns></returns>
    [HttpPut("CommandTemplate")]
    [RequestSizeLimit(1520)]
    [ProducesResponseType(typeof(ExtendedCommandTemplateExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ModifyCommandTemplate(ModifyCommandTemplateModel model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"ModifyCommandTemplate\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);
        var commandTemplate = _managementService.ModifyCommandTemplateModel(model.Id, model.Name, model.Description,
            model.ExtendedAllocationCommand, model.ExecutableFile, model.PreparationScript, model.ClusterNodeTypeId,
            model.IsEnabled, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(commandTemplate);
    }

    /// <summary>
    ///     Modify Command Template based on Generic Command Template
    /// </summary>
    /// <param name="fromGenericModel">ModifyCommandTemplateFromGenericModel</param>
    /// <returns></returns>
    [HttpPut("CommandTemplateFromGeneric")]
    [RequestSizeLimit(1520)]
    [ProducesResponseType(typeof(CommandTemplateExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ModifyCommandTemplateFromGeneric(ModifyCommandTemplateFromGenericModel fromGenericModel)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"ModifyCommandTemplateFromGeneric\"");
        var validationResult = new ManagementValidator(fromGenericModel).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);
        var commandTemplate = _managementService.ModifyCommandTemplateFromGeneric(fromGenericModel.CommandTemplateId,
            fromGenericModel.Name, fromGenericModel.ProjectId, fromGenericModel.Description,
            fromGenericModel.ExtendedAllocationCommand,
            fromGenericModel.ExecutableFile, fromGenericModel.PreparationScript, fromGenericModel.SessionCode);
        ClearListAvailableClusterMethodCache(fromGenericModel.SessionCode);
        return Ok(commandTemplate);
    }

    /// <summary>
    ///     Remove Command Template
    /// </summary>
    /// <param name="model">RemoveCommandTemplateModel</param>
    /// <returns></returns>
    [HttpDelete("RemoveCommandTemplate")]
    [RequestSizeLimit(90)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveCommandTemplate(RemoveCommandTemplateModel model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"RemoveCommandTemplateModel\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        ClearListAvailableClusterMethodCache(model.SessionCode);
        _managementService.RemoveCommandTemplate(model.CommandTemplateId, model.SessionCode);
        return Ok("CommandTemplate was deleted.");
    }

    #endregion

    #region CommandTemplateParameter

    /// <summary>
    ///     Get CommandTemplateParameter by id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("CommandTemplateParameter")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(ExtendedCommandTemplateParameterExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetCommandTemplateParameterById(long id, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetCommandTemplateParameterById\" Parameters: Id: \"{id}\", SessionCode: \"{sessionCode}\"");

        var commandTemplateParameter = _managementService.GetCommandTemplateParameterById(id, sessionCode);
        return Ok(commandTemplateParameter);
    }

    /// <summary>
    ///     Create Static Command Template
    /// </summary>
    /// <param name="model">CreateCommandTemplateModel</param>
    /// <returns></returns>
    [HttpPost("CommandTemplateParameter")]
    [RequestSizeLimit(500)]
    [ProducesResponseType(typeof(ExtendedCommandTemplateParameterExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateCommandTemplateParameter(CreateCommandTemplateParameterModel model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"CreateCommandTemplateParameter\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);
        var commandTemplateParameter = _managementService.CreateCommandTemplateParameter(model.Identifier, model.Query,
            model.Description, model.CommandTemplateId, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(commandTemplateParameter);
    }

    /// <summary>
    ///     Modify Static Command Template
    /// </summary>
    /// <param name="model">ModifyCommandTemplateParameterModel</param>
    /// <returns></returns>
    [HttpPut("CommandTemplateParameter")]
    [RequestSizeLimit(500)]
    [ProducesResponseType(typeof(ExtendedCommandTemplateParameterExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ModifyCommandTemplateParameter(ModifyCommandTemplateParameterModel model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"ModifyCommandTemplateParameter\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);
        var commandTemplateParameter = _managementService.ModifyCommandTemplateParameter(model.Id, model.Identifier,
            model.Query, model.Description, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(commandTemplateParameter);
    }


    /// <summary>
    ///     Remove Static Command Template
    /// </summary>
    /// <param name="model">RemoveCommandTemplateParameterModel</param>
    /// <returns></returns>
    [HttpDelete("CommandTemplateParameter")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveCommandTemplateParameter(RemoveCommandTemplateParameterModel model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"RemoveCommandTemplateParameter\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var message = _managementService.RemoveCommandTemplateParameter(model.Id, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(message);
    }

    #endregion

    #region Project

    /// <summary>
    ///     Get project by accounting string
    /// </summary>
    /// <param name="accountingString"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("ProjectsByAccountingStrings")]
    [RequestSizeLimit(3000)]
    [ProducesResponseType(typeof(List<ProjectExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetProjectsByAccountingStrings([FromQuery] string[] accountingString, string sessionCode)
    {
        List<ProjectExt> projects = new();
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetProjectsByAccountingStrings\" Parameters: AccountingString: \"{string.Join(",", accountingString)}\", SessionCode: \"{sessionCode}\"");

        foreach (var element in accountingString)
            try
            {
                var project = _managementService.GetProjectByAccountingString(element, sessionCode);
                projects.Add(project);
                _logger.LogDebug($"Project with accounting string \"{element}\" found.");
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Project with accounting string \"{element}\" not found. {e.Message}");
            }

        return Ok(projects);
    }

    /// <summary>
    ///     Get Project by id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("Project")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(ProjectExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetProjectById(long id, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetProjectById\" Parameters: Id: \"{id}\", SessionCode: \"{sessionCode}\"");

        var project = _managementService.GetProjectById(id, sessionCode);
        return Ok(project);
    }

    /// <summary>
    ///     Create project
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("Project")]
    [RequestSizeLimit(600)]
    [ProducesResponseType(typeof(ProjectExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateProject(CreateProjectModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"CreateProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var project = _managementService.CreateProject(model.AccountingString,
            model.UsageType.HasValue ? model.UsageType.ConvertExtToInt() : UsageType.NodeHours,
            model.Name, model.Description,
            model.StartDate, model.EndDate, model.UseAccountingStringForScheduler,
            model.PIEmail, model.IsOneToOneMapping ?? false,
            model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(project);
    }

    /// <summary>
    ///     Modify project
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("Project")]
    [RequestSizeLimit(600)]
    [ProducesResponseType(typeof(ProjectExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ModifyProject(ModifyProjectModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"ModifyProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var project = _managementService.ModifyProject(model.Id, model.UsageType.ConvertExtToInt(), model.Name,
            model.Description, model.StartDate, model.EndDate, model.UseAccountingStringForScheduler,
            model.IsOneToOneMapping ?? false, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(project);
    }

    /// <summary>
    ///     Remove project
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("Project")]
    [RequestSizeLimit(600)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveProject(RemoveProjectModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"RemoveProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        ClearListAvailableClusterMethodCache(model.SessionCode);
        _managementService.RemoveProject(model.Id, model.SessionCode);
        return Ok("Project was deleted.");
    }

    #endregion

    #region ProjectAssignmentToCluster

    /// <summary>
    ///     Get ProjectAssignmentToCluster by id
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="clusterId"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("ProjectAssignmentToCluster")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(ClusterProjectExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetProjectAssignmentToClusterById(long projectId, long clusterId, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetProjectAssignmentToClusterById\" Parameters: ProjectId: \"{projectId}\", ClusterId: \"{clusterId}\", SessionCode: \"{sessionCode}\"");

        var clusterProject = _managementService.GetProjectAssignmentToClusterById(projectId, clusterId, sessionCode);
        return Ok(clusterProject);
    }

    /// <summary>
    ///     Assign project to the cluster
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("ProjectAssignmentToCluster")]
    [RequestSizeLimit(600)]
    [ProducesResponseType(typeof(ClusterProjectExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateProjectAssignmentToCluster(CreateProjectAssignmentToClusterModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"CreateProjectAssignmentToCluster\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var clusterProject = _managementService.CreateProjectAssignmentToCluster(model.ProjectId, model.ClusterId,
            model.LocalBasepath, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(clusterProject);
    }

    /// <summary>
    ///     Modify project assignment to the cluster
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("ProjectAssignmentToCluster")]
    [RequestSizeLimit(600)]
    [ProducesResponseType(typeof(ClusterProjectExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ModifyProjectAssignmentToCluster(ModifyProjectAssignmentToClusterModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"ModifyProjectAssignmentToCluster\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var clusterProject = _managementService.ModifyProjectAssignmentToCluster(model.ProjectId, model.ClusterId,
            model.LocalBasepath, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(clusterProject);
    }

    /// <summary>
    ///     Remove project assignment to the cluster
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("ProjectAssignmentToCluster")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveProjectAssignmentToCluster(RemoveProjectAssignmentToClusterModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"RemoveProjectAssignmentToCluster\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _managementService.RemoveProjectAssignmentToCluster(model.ProjectId, model.ClusterId, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok("Removed assignment of the Project to the Cluster.");
    }

    #endregion

    #region Cluster

    /// <summary>
    ///     Get Cluster by id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("Cluster")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(ExtendedClusterExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetClusterById(long id, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetClusterById\" Parameters: Id: \"{id}\", SessionCode: \"{sessionCode}\"");

        var cluster = _managementService.GetClusterById(id, sessionCode);
        return Ok(cluster);
    }

    /// <summary>
    ///     Create Cluster
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("Cluster")]
    [RequestSizeLimit(600)]
    [ProducesResponseType(typeof(ExtendedClusterExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateCluster(CreateClusterModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"CreateCluster\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var cluster = _managementService.CreateCluster(model.Name, model.Description, model.MasterNodeName,
            model.SchedulerType, model.ConnectionProtocol,
            model.TimeZone, model.Port, model.UpdateJobStateByServiceAccount, model.DomainName, model.ProxyConnectionId,
            model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(cluster);
    }

    /// <summary>
    ///     Update Cluster
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("Cluster")]
    [RequestSizeLimit(600)]
    [ProducesResponseType(typeof(ExtendedClusterExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ModifyCluster(ModifyClusterModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"ModifyCluster\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var cluster = _managementService.ModifyCluster(model.Id, model.Name, model.Description, model.MasterNodeName,
            model.SchedulerType, model.ConnectionProtocol,
            model.TimeZone, model.Port, model.UpdateJobStateByServiceAccount, model.DomainName, model.ProxyConnectionId,
            model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(cluster);
    }

    /// <summary>
    ///     Remove Cluster
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("Cluster")]
    [RequestSizeLimit(90)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveCluster(RemoveClusterModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"RemoveCluster\" Parameters: Id: \"{model.Id}\", SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _managementService.RemoveCluster(model.Id, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok("Cluster was deleted.");
    }
    
    /// <summary>
    ///     Get all clusters
    /// </summary>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("Clusters")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(List<ExtendedClusterExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetAllClusters(string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetAllClusters\", SessionCode: \"{sessionCode}\"");

        var clusters = _managementService.GetClusters(sessionCode);
        return Ok(clusters);
    }

    #endregion

    #region ClusterNodeType

    /// <summary>
    ///     Get ClusterNodeType by id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("ClusterNodeType")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(ClusterNodeTypeExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetClusterNodeTypeById(long id, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetClusterNodeTypeById\" Parameters: Id: \"{id}\", SessionCode: \"{sessionCode}\"");

        var clusterNodeType = _managementService.GetClusterNodeTypeById(id, sessionCode);
        return Ok(clusterNodeType);
    }

    /// <summary>
    ///     Create ClusterNodeType
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("ClusterNodeType")]
    [RequestSizeLimit(600)]
    [ProducesResponseType(typeof(ClusterNodeTypeExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateClusterNodeType(CreateClusterNodeTypeModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"CreateClusterNodeType\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var clusterNodeType = _managementService.CreateClusterNodeType(model.Name, model.Description,
            model.NumberOfNodes, model.CoresPerNode, model.Queue, model.QualityOfService,
            model.MaxWalltime, model.ClusterAllocationName, model.ClusterId, model.FileTransferMethodId,
            model.ClusterNodeTypeAggregationId, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(clusterNodeType);
    }

    /// <summary>
    ///     Modify ClusterNodeType
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("ClusterNodeType")]
    [RequestSizeLimit(600)]
    [ProducesResponseType(typeof(ClusterNodeTypeExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ModifyClusterNodeType(ModifyClusterNodeTypeModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"ModifyClusterNodeType\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var clusterNodeType = _managementService.ModifyClusterNodeType(model.Id, model.Name, model.Description,
            model.NumberOfNodes, model.CoresPerNode, model.Queue,
            model.QualityOfService, model.MaxWalltime, model.ClusterAllocationName, model.ClusterId,
            model.FileTransferMethodId, model.ClusterNodeTypeAggregationId,
            model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(clusterNodeType);
    }

    /// <summary>
    ///     Remove ClusterNodeType
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("ClusterNodeType")]
    [RequestSizeLimit(90)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveClusterNodeType(RemoveClusterNodeTypeModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"RemoveClusterNodeType\" Parameters: Id: \"{model.Id}\", SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _managementService.RemoveClusterNodeType(model.Id, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok("ClusterNodeType was deleted.");
    }

    #endregion

    #region ClusterProxyConnection

    /// <summary>
    ///     Get ClusterProxyConnection by id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("ClusterProxyConnection")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(ClusterProxyConnectionExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetClusterProxyConnectionById(long id, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetClusterProxyConnectionById\" Parameters: Id: \"{id}\", SessionCode: \"{sessionCode}\"");

        var clusterProxyConnection = _managementService.GetClusterProxyConnectionById(id, sessionCode);
        return Ok(clusterProxyConnection);
    }

    /// <summary>
    ///     Create ClusterProxyConnection
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("ClusterProxyConnection")]
    [RequestSizeLimit(300)]
    [ProducesResponseType(typeof(ClusterProxyConnectionExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateClusterProxyConnection(CreateClusterProxyConnectionModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"CreateClusterProxyConnection\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var clusterProxyConnection = _managementService.CreateClusterProxyConnection(model.Host, model.Port,
            model.Username, model.Password, model.Type, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(clusterProxyConnection);
    }

    /// <summary>
    ///     Modify ClusterProxyConnection
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("ClusterProxyConnection")]
    [RequestSizeLimit(300)]
    [ProducesResponseType(typeof(ClusterProxyConnectionExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ModifyClusterProxyConnection(ModifyClusterProxyConnectionModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"ModifyClusterProxyConnection\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var clusterProxyConnection = _managementService.ModifyClusterProxyConnection(model.Id, model.Host, model.Port,
            model.Username, model.Password, model.Type,
            model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(clusterProxyConnection);
    }

    /// <summary>
    ///     Remove ClusterProxyConnection
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("ClusterProxyConnection")]
    [RequestSizeLimit(90)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveClusterProxyConnection(RemoveClusterProxyConnectionModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"RemoveClusterProxyConnection\" Parameters: Id: \"{model.Id}\", SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _managementService.RemoveClusterProxyConnection(model.Id, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok("ClusterProxyConnection was deleted.");
    }

    #endregion

    #region FileTransferMethod

    /// <summary>
    ///     Get FileTransferMethod by id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("FileTransferMethod")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(FileTransferMethodNoCredentialsExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetFileTransferMethodById(long id, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetFileTransferMethodById\" Parameters: Id: \"{id}\", SessionCode: \"{sessionCode}\"");

        var fileTransferMethod = _managementService.GetFileTransferMethodById(id, sessionCode);
        return Ok(fileTransferMethod);
    }

    /// <summary>
    ///     Create FileTransferMethod
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("FileTransferMethod")]
    [RequestSizeLimit(300)]
    [ProducesResponseType(typeof(FileTransferMethodNoCredentialsExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateFileTransferMethod(CreateFileTransferMethodModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"CreateFileTransferMethod\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var fileTransferMethod = _managementService.CreateFileTransferMethod(model.ServerHostname, model.Protocol,
            model.ClusterId, model.Port, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(fileTransferMethod);
    }

    /// <summary>
    ///     Modify FileTransferMethod
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("FileTransferMethod")]
    [RequestSizeLimit(300)]
    [ProducesResponseType(typeof(FileTransferMethodNoCredentialsExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ModifyFileTransferMethod(ModifyFileTransferMethodModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"ModifyFileTransferMethod\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var fileTransferMethod = _managementService.ModifyFileTransferMethod(model.Id, model.ServerHostname,
            model.Protocol, model.ClusterId, model.Port, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(fileTransferMethod);
    }

    /// <summary>
    ///     Remove FileTransferMethod
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("FileTransferMethod")]
    [RequestSizeLimit(90)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveFileTransferMethod(RemoveFileTransferMethodModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"RemoveFileTransferMethod\" Parameters: Id: \"{model.Id}\", SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _managementService.RemoveFileTransferMethod(model.Id, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok("FileTransferMethod was deleted.");
    }

    #endregion

    #region ClusterNodeTypeAggregation

    /// <summary>
    ///     Get ClusterNodeTypeAggregation by id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("ClusterNodeTypeAggregation")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(ClusterNodeTypeAggregationExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetClusterNodeTypeAggregationById(long id, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetClusterNodeTypeAggregationById\" Parameters: Id: \"{id}\", SessionCode: \"{sessionCode}\"");

        var clusterNodeTypeAggregation = _managementService.GetClusterNodeTypeAggregationById(id, sessionCode);
        return Ok(clusterNodeTypeAggregation);
    }

    /// <summary>
    ///     Get all ClusterNodeTypeAggregations
    /// </summary>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("ClusterNodeTypeAggregations")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(List<ClusterNodeTypeAggregationExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetClusterNodeTypeAggregations(string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetClusterNodeTypeAggregations\" Parameters: SessionCode: \"{sessionCode}\"");

        var clusterNodeTypeAggregations = _managementService.GetClusterNodeTypeAggregations(sessionCode);
        return Ok(clusterNodeTypeAggregations);
    }

    /// <summary>
    ///     Create ClusterNodeTypeAggregations
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("ClusterNodeTypeAggregation")]
    [RequestSizeLimit(300)]
    [ProducesResponseType(typeof(ClusterNodeTypeAggregationExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateClusterNodeTypeAggregation(CreateClusterNodeTypeAggregationModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"CreateClusterNodeTypeAggregation\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var clusterNodeTypeAggregation = _managementService.CreateClusterNodeTypeAggregation(model.Name,
            model.Description, model.AllocationType, model.ValidityFrom,
            model.ValidityTo, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(clusterNodeTypeAggregation);
    }

    /// <summary>
    ///     Modify ClusterNodeTypeAggregations
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("ClusterNodeTypeAggregation")]
    [RequestSizeLimit(300)]
    [ProducesResponseType(typeof(ClusterNodeTypeAggregationExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ModifyClusterNodeTypeAggregation(ModifyClusterNodeTypeAggregationModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"ModifyClusterNodeTypeAggregation\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var clusterNodeTypeAggregation = _managementService.ModifyClusterNodeTypeAggregation(model.Id, model.Name,
            model.Description, model.AllocationType, model.ValidityFrom,
            model.ValidityTo, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(clusterNodeTypeAggregation);
    }

    /// <summary>
    ///     Remove ClusterNodeTypeAggregation
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("ClusterNodeTypeAggregation")]
    [RequestSizeLimit(90)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveClusterNodeTypeAggregation(RemoveClusterNodeTypeAggregationModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"RemoveClusterNodeTypeAggregation\" Parameters: Id: \"{model.Id}\", SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _managementService.RemoveClusterNodeTypeAggregation(model.Id, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok("ClusterNodeTypeAggregation was deleted.");
    }

    #endregion

    #region ClusterNodeTypeAggregationAccounting

    /// <summary>
    ///     Get ClusterNodeTypeAggregationAccounting by id
    /// </summary>
    /// <param name="clusterNodeTypeAggregationId"></param>
    /// <param name="accountingId"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("ClusterNodeTypeAggregationAccounting")]
    [RequestSizeLimit(120)]
    [ProducesResponseType(typeof(ClusterNodeTypeAggregationAccountingExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetClusterNodeTypeAggregationAccountingById(long clusterNodeTypeAggregationId,
        long accountingId, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetClusterNodeTypeAggregationAccountingById\" Parameters: ClusterNodeTypeAggregationId: \"{clusterNodeTypeAggregationId}\" , AccountingId: \"{accountingId}\", SessionCode: \"{sessionCode}\"");

        var clusterNodeTypeAggregationAccounting =
            _managementService.GetClusterNodeTypeAggregationAccountingById(clusterNodeTypeAggregationId, accountingId,
                sessionCode);
        return Ok(clusterNodeTypeAggregationAccounting);
    }

    /// <summary>
    ///     Create ClusterNodeTypeAggregationAccounting
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("ClusterNodeTypeAggregationAccounting")]
    [RequestSizeLimit(300)]
    [ProducesResponseType(typeof(ClusterNodeTypeAggregationAccountingExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateClusterNodeTypeAggregationAccounting(
        CreateClusterNodeTypeAggregationAccountingModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"CreateClusterNodeTypeAggregationAccounting\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var clusterNodeTypeAggregationAccounting =
            _managementService.CreateClusterNodeTypeAggregationAccounting(model.ClusterNodeTypeAggregationId,
                model.AccountingId, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(clusterNodeTypeAggregationAccounting);
    }

    /// <summary>
    ///     Remove ClusterNodeTypeAggregationAccounting
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("ClusterNodeTypeAggregationAccounting")]
    [RequestSizeLimit(120)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveClusterNodeTypeAggregationAccounting(
        RemoveClusterNodeTypeAggregationAccountingModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"RemoveClusterNodeTypeAggregationAccounting\" Parameters: ClusterNodeTypeAggregationId: \"{model.ClusterNodeTypeAggregationId}\" , AccountingId: \"{model.AccountingId}\", SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _managementService.RemoveClusterNodeTypeAggregationAccounting(model.ClusterNodeTypeAggregationId,
            model.AccountingId, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok("ClusterNodeTypeAggregationAccounting was deleted.");
    }

    #endregion

    #region Accounting

    /// <summary>
    ///     Get Accounting by id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("Accounting")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(AccountingExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetAccountingById(long id, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetAccountingById\" Parameters: Id: \"{id}\", SessionCode: \"{sessionCode}\"");

        var accounting = _managementService.GetAccountingById(id, sessionCode);
        return Ok(accounting);
    }

    /// <summary>
    ///     Create Accounting
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("Accounting")]
    [RequestSizeLimit(300)]
    [ProducesResponseType(typeof(AccountingExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateAccounting(CreateAccountingModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"CreateAccounting\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var accounting = _managementService.CreateAccounting(model.Formula, model.ValidityFrom, model.ValidityTo, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(accounting);
    }

    /// <summary>
    ///     Modify Accounting
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("Accounting")]
    [RequestSizeLimit(300)]
    [ProducesResponseType(typeof(AccountingExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ModifyAccounting(ModifyAccountingModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"ModifyAccounting\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var accounting = _managementService.ModifyAccounting(model.Id, model.Formula, model.ValidityFrom,
            model.ValidityTo, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(accounting);
    }

    /// <summary>
    ///     Remove Accounting
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("Accounting")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveAccounting(RemoveAccountingModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"RemoveAccounting\" Parameters: Id: \"{model.Id}\", SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _managementService.RemoveAccounting(model.Id, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok("Accounting was deleted.");
    }

    #endregion

    #region ProjectClusterNodeTypeAggregation

    /// <summary>
    ///     Get ProjectClusterNodeTypeAggregation by id
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="clusterNodeTypeAggregationId"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("ProjectClusterNodeTypeAggregation")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(ProjectClusterNodeTypeAggregationExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetProjectClusterNodeTypeAggregationById(long projectId, long clusterNodeTypeAggregationId,
        string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetProjectClusterNodeTypeAggregationById\" Parameters: ProjectId: \"{projectId}\", ClusterNodeTypeAggregationId: \"{clusterNodeTypeAggregationId}\", SessionCode: \"{sessionCode}\"");

        var projectClusterNodeTypeAggregation =
            _managementService.GetProjectClusterNodeTypeAggregationById(projectId, clusterNodeTypeAggregationId,
                sessionCode);
        return Ok(projectClusterNodeTypeAggregation);
    }

    /// <summary>
    ///     Get ProjectClusterNodeTypeAggregations by ProjectId
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("ProjectClusterNodeTypeAggregations")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(List<ProjectClusterNodeTypeAggregationExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetProjectClusterNodeTypeAggregationsByProjectId(long projectId, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetProjectClusterNodeTypeAggregationsByProjectId\" Parameters: ProjectId: \"{projectId}\", SessionCode: \"{sessionCode}\"");

        var projectClusterNodeTypeAggregations =
            _managementService.GetProjectClusterNodeTypeAggregationsByProjectId(projectId, sessionCode);
        return Ok(projectClusterNodeTypeAggregations);
    }

    /// <summary>
    ///     Create ProjectClusterNodeTypeAggregation
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("ProjectClusterNodeTypeAggregation")]
    [RequestSizeLimit(300)]
    [ProducesResponseType(typeof(ProjectClusterNodeTypeAggregationExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateProjectClusterNodeTypeAggregation(CreateProjectClusterNodeTypeAggregationModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"CreateProjectClusterNodeTypeAggregation\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var projectClusterNodeTypeAggregation = _managementService.CreateProjectClusterNodeTypeAggregation(
            model.ProjectId, model.ClusterNodeTypeAggregationId, model.AllocationAmount, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(projectClusterNodeTypeAggregation);
    }

    /// <summary>
    ///     Modify ProjectClusterNodeTypeAggregation
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("ProjectClusterNodeTypeAggregation")]
    [RequestSizeLimit(300)]
    [ProducesResponseType(typeof(ProjectClusterNodeTypeAggregationExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ModifyProjectClusterNodeTypeAggregation(ModifyProjectClusterNodeTypeAggregationModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"ModifyProjectClusterNodeTypeAggregation\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var projectClusterNodeTypeAggregation = _managementService.ModifyProjectClusterNodeTypeAggregation(
            model.ProjectId, model.ClusterNodeTypeAggregationId, model.AllocationAmount, model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok(projectClusterNodeTypeAggregation);
    }

    /// <summary>
    ///     Remove ProjectClusterNodeTypeAggregation
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("ProjectClusterNodeTypeAggregation")]
    [RequestSizeLimit(120)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveProjectClusterNodeTypeAggregation(RemoveProjectClusterNodeTypeAggregationModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"RemoveProjectClusterNodeTypeAggregation\" Parameters: ProjectId: \"{model.ProjectId}\", ClusterNodeTypeAggregationId: \"{model.ClusterNodeTypeAggregationId}\", SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _managementService.RemoveProjectClusterNodeTypeAggregation(model.ProjectId, model.ClusterNodeTypeAggregationId,
            model.SessionCode);
        ClearListAvailableClusterMethodCache(model.SessionCode);
        return Ok("ProjectClusterNodeTypeAggregation was deleted.");
    }

    #endregion

    #region SubProject

    /// <summary>
    ///     List SubProjects
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    /// <exception cref="InputValidationException"></exception>
    [HttpGet("SubProjects")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(List<SubProjectExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ListSubProjects(long projectId, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"ListSubProjects\" Parameters: SessionCode: \"{sessionCode}\"");
        var model = new ListSubProjectsModel
        {
            Id = projectId,
            SessionCode = sessionCode
        };
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_managementService.ListSubProjects(projectId, sessionCode));
    }

    /// <summary>
    ///     List SubProject
    /// </summary>
    /// <param name="subProjectId"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    /// <exception cref="InputValidationException"></exception>
    [HttpGet("SubProject")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(SubProjectExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ListSubProject(long subProjectId, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"ListSubProject\" Parameters: SessionCode: \"{sessionCode}\"");
        var model = new ListSubProjectModel
        {
            Id = subProjectId,
            SessionCode = sessionCode
        };
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_managementService.ListSubProject(subProjectId, sessionCode));
    }

    /// <summary>
    ///     Create SubProject
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    /// <exception cref="InputValidationException"></exception>
    [HttpPost("SubProject")]
    [RequestSizeLimit(1000)]
    [ProducesResponseType(typeof(SubProjectExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateSubProject(CreateSubProjectModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"CreateSubProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var subProject = _managementService.CreateSubProject(model.ProjectId, model.Identifier, model.Description,
            model.StartDate, model.EndDate, model.SessionCode);
        return Ok(subProject);
    }

    /// <summary>
    ///     Modify SubProject
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    /// <exception cref="InputValidationException"></exception>
    [HttpPut("SubProject")]
    [RequestSizeLimit(1000)]
    [ProducesResponseType(typeof(SubProjectExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ModifySubProject(ModifySubProjectModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"ModifySubProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var subProject = _managementService.ModifySubProject(model.Id, model.Identifier, model.Description,
            model.StartDate, model.EndDate, model.SessionCode);
        return Ok(subProject);
    }

    /// <summary>
    ///     Remove SubProject
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    /// <exception cref="InputValidationException"></exception>
    [HttpDelete("SubProject")]
    [RequestSizeLimit(100)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveSubProject(RemoveSubProjectModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"RemoveSubProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _managementService.RemoveSubProject(model.Id, model.SessionCode);
        return Ok("SubProject was deleted.");
    }

    #endregion

    #region SecureShellKey

    /// <summary>
    ///     Generate SSH key
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("SecureShellKey")]
    [RequestSizeLimit(300)]
    [ProducesResponseType(typeof(List<PublicKeyExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Obsolete]
    public IActionResult CreateSecureShellKeyObsolete(CreateSecureShellKeyModelObsolete model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"CreateSecureShellKey\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);
        List<(string, string)> usernamePasswords = new()
        {
            (model.Username, model.Password)
        };

        return Ok(_managementService.CreateSecureShellKey(usernamePasswords, model.ProjectId, model.SessionCode));
    }

    /// <summary>
    ///     Get SSH keys for project
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("SecureShellKeys")]
    [RequestSizeLimit(1000)]
    [ProducesResponseType(typeof(List<PublicKeyExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetSecureShellKeys(long projectId, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetSecureShellKeys\" Parameters: ProjectId: \"{projectId}\", SessionCode: \"{sessionCode}\"");
        GetSecureShellKeysModel model = new()
        {
            ProjectId = projectId,
            SessionCode = sessionCode
        };
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_managementService.GetSecureShellKeys(model.ProjectId, model.SessionCode));
    }

    /// <summary>
    ///     Generate SSH key
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("GenerateSecureShellKey")]
    [RequestSizeLimit(1000)]
    [ProducesResponseType(typeof(List<PublicKeyExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GenerateSecureShellKey(CreateSecureShellKeyModel model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"GenerateSecureShellKey\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        List<(string, string)> credentials =
            model.Credentials.Select(credential => (credential.Username, credential.Password)).ToList();
        return Ok(_managementService.CreateSecureShellKey(credentials, model.ProjectId, model.SessionCode));
    }

    /// <summary>
    ///     Regenerate SSH key
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("SecureShellKey")]
    [RequestSizeLimit(1000)]
    [ProducesResponseType(typeof(PublicKeyExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Obsolete]
    public IActionResult RecreateSecureShellKey(RegenerateSecureShellKeyModelObsolete model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"RecreateSecureShellKey\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_managementService.RegenerateSecureShellKey(string.Empty, model.Password, model.PublicKey,
            model.ProjectId, model.SessionCode));
    }

    /// <summary>
    ///     Regenerate SSH key
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("RegenerateSecureShellKey")]
    [RequestSizeLimit(1000)]
    [ProducesResponseType(typeof(PublicKeyExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RegenerateSecureShellKey(RegenerateSecureShellKeyModel model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"RecreateSecureShellKey\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_managementService.RegenerateSecureShellKey(model.Username, model.Password, string.Empty,
            model.ProjectId, model.SessionCode));
    }

    /// <summary>
    ///     Remove SSH key
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("SecureShellKey")]
    [RequestSizeLimit(1000)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Obsolete]
    public IActionResult RemoveSecureShellKeyObsolete(RemoveSecureShellKeyModelObsolete model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"RevokeSecureShellKey\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _managementService.RemoveSecureShellKey(null, model.PublicKey, model.ProjectId, model.SessionCode);
        return Ok("SecureShellKey revoked");
    }

    /// <summary>
    ///     Remove SSH key
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("RemoveSecureShellKey")]
    [RequestSizeLimit(1000)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveSecureShellKey(RemoveSecureShellKeyModel model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"RevokeSecureShellKey\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _managementService.RemoveSecureShellKey(model.Username, null, model.ProjectId, model.SessionCode);
        return Ok("SecureShellKey revoked");
    }

    #endregion

    /// <summary>
    ///     Initialize cluster script directory for SSH HPC Account
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("InitializeClusterScriptDirectory")]
    [RequestSizeLimit(1000)]
    [ProducesResponseType(typeof(List<ClusterInitReportExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult InitializeClusterScriptDirectory(InitializeClusterScriptDirectoryModel model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"InitializeClusterScriptDirectory\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        List<ClusterInitReportExt> report = _managementService.InitializeClusterScriptDirectory(model.ProjectId,
            model.ClusterProjectRootDirectory, model.SessionCode, model.Username);
        
        if(report.Any(x=> !x.IsClusterInitialized))
            return BadRequest(report);
        return Ok(report);
    }

    /// <summary>
    ///     Test cluster access for robot account
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("TestClusterAccessForAccount")]
    [RequestSizeLimit(1000)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Obsolete]
    public IActionResult TestClusterAccessForAccountObsolete(TestClusterAccessForAccountModelObsolete model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"TestClusterAccessForAccount\"");
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var testClusterAccess =
            _managementService.TestClusterAccessForAccount(model.ProjectId, model.SessionCode, null);
        var message = testClusterAccess
            ? "All clusters assigned to project are accessible with selected account."
            : "Some of the clusters are not accessible with selected account";

        _logger.LogInformation(message);
        return testClusterAccess ? Ok(message) : BadRequest(message);
    }

    /// <summary>
    ///     Test cluster access for robot account
    /// </summary>
    /// <param name="username"></param>
    /// <param name="projectId"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("TestClusterAccessForAccount")]
    [RequestSizeLimit(1000)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult TestClusterAccessForAccount(string username, long projectId, string sessionCode)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"TestClusterAccessForAccount\"");

        var validationResult = new ManagementValidator(new TestClusterAccessForAccountModel
        {
            ProjectId = projectId,
            SessionCode = sessionCode,
            Username = username
        }).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var testClusterAccess = _managementService.TestClusterAccessForAccount(projectId, sessionCode, username);
        var message = testClusterAccess
            ? "All clusters assigned to project are accessible with selected account."
            : "Some of the clusters are not accessible with selected account";

        _logger.LogInformation(message);
        return testClusterAccess ? Ok(message) : BadRequest(message);
    }

    /// <summary>
    ///     Compute accounting - calculate accounting via accounting formulas
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    /// <exception cref="InputValidationException"></exception>
    [HttpPost("ComputeAccounting")]
    [RequestSizeLimit(500)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ComputeAccounting(ComputeAccountingModel model)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"ComputeAccounting\"");

        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _managementService.ComputeAccounting(model.StartTime, model.EndTime, model.ProjectId, model.SessionCode);
        return Ok($"Accounting triggered for project {model.ProjectId}.");
    }


    [HttpPost("AccountingState")]
    [RequestSizeLimit(200)]
    [ProducesResponseType(typeof(AccountingStateExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ListAccountingStates(long projectId, string sessionCode)
    {
        _logger.LogDebug("Endpoint: \"Management\" Method: \"AccountingState\"");
        var model = new AccountingStateModel
        {
            ProjectId = projectId,
            SessionCode = sessionCode
        };
        var validationResult = new ManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);
        return Ok(_managementService.ListAccountingStates(projectId, sessionCode));
    }

    #endregion
}