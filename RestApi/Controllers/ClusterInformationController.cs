using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using HEAppE.Authentication;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.RestApi.Authentication;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.ClusterInformation;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using HEAppE.ServiceTier.ClusterInformation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.RestApi.Controllers;

/// <summary>
///     Cluster information Endpoint
/// </summary>
[ApiController]
[Route("heappe/[controller]")]
[Produces("application/json")]
public class ClusterInformationController : BaseController<ClusterInformationController>
{
    #region Instances

    private readonly IClusterInformationService _service;

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="cacheProvider">Memory cache instance</param>
    /// <param name="userOrgService"></param>
    /// <param name="httpContextKeys"></param>
    /// <param name="sshCertificateAuthorityService">SSH Certificate Authority service</param>
    public ClusterInformationController(ILogger<ClusterInformationController> logger, IMemoryCache cacheProvider, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, 
                                        IHttpContextKeys httpContextKeys, IExpirioService expirioService) : base(logger, cacheProvider)
    {
        _service = new ClusterInformationService(cacheProvider, userOrgService, sshCertificateAuthorityService, httpContextKeys, expirioService, logger);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Get available clusters
    /// </summary>
    /// <param name="sessionCode"></param>
    /// <param name="clusterName"></param>
    /// <param name="nodeTypeName"></param>
    /// <param name="projectName"></param>
    /// <param name="accountingString"></param>
    /// <param name="commandTemplateName"></param>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    [HttpGet("ListAvailableClusters")]
    [RequestSizeLimit(0)]
    [ProducesResponseType(typeof(IEnumerable<ClusterExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListAvailableClusters(string sessionCode, string clusterName = null, string nodeTypeName = null,
        string projectName = null, [FromQuery] string[] accountingString = null, string commandTemplateName = null, bool? forceRefresh = null)
    {
        ListAvailableClustersModel model = new()
        {
            SessionCode = sessionCode
        };
        var validationResult = new ClusterInformationValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);
        return Ok(await _service.ListAvailableClusters(sessionCode, clusterName, nodeTypeName, projectName, accountingString,
            commandTemplateName, forceRefresh ?? false));
    }

    /// <summary>
    ///     Get command template parameters name
    /// </summary>
    /// <returns></returns>
    [HttpPost("RequestCommandTemplateParametersName")]
    [RequestSizeLimit(535)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RequestCommandTemplateParametersName(GetCommandTemplateParametersNameModel model)
    {
        var validationResult = new ClusterInformationValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.RequestCommandTemplateParametersName(model.CommandTemplateId, model.ProjectId,
            model.UserScriptPath, model.SessionCode));
    }

    /// <summary>
    ///     Get actual cluster node usage
    /// </summary>
    /// <param name="sessionCode">Session code</param>
    /// <param name="clusterNodeId">ClusterNode ID</param>
    /// <param name="projectId">Project ID</param>
    /// <returns></returns>
    [HttpGet("CurrentClusterNodeUsage")]
    [RequestSizeLimit(154)]
    [ProducesResponseType(typeof(ClusterNodeUsageExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CurrentClusterNodeUsage(string sessionCode, long clusterNodeId, long projectId)
    {
        var model = new CurrentClusterNodeUsageModel
        {
            SessionCode = sessionCode,
            ClusterNodeId = clusterNodeId,
            ProjectId = projectId
        };
        var validationResult = new ClusterInformationValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.GetCurrentClusterNodeUsage(model.ClusterNodeId, model.ProjectId, model.SessionCode));
    }

    /// <summary>
    ///     Get QScheduler machine architecture
    /// </summary>
    /// <returns>JSON string with machine architecture topology</returns>
    [HttpGet("MachineArchitecture")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MachineArchitecture([FromQuery] GetMachineArchitectureModel model)
    {
        _logger.LogInformation($"MachineArchitecture API request received. ClusterNodeTypeId: {model?.ClusterNodeTypeId}, ProjectId: {model?.ProjectId}");
        var validationResult = new ClusterInformationValidator(model).Validate();
        if (!validationResult.IsValid)
        {
            _logger.LogWarning($"MachineArchitecture validation failed: {validationResult.Message}");
            throw new InputValidationException(validationResult.Message);
        }

        var result = await _service.GetMachineArchitecture(model.ClusterNodeTypeId, model.ProjectId, model.SessionCode);
        _logger.LogInformation($"MachineArchitecture API request completed. ClusterNodeTypeId: {model.ClusterNodeTypeId}");
        return Content(result, "application/json");
    }

    /// <summary>
    ///     Get QScheduler machine calibration
    /// </summary>
    /// <returns>JSON string with machine calibration parameters</returns>
    [HttpGet("MachineCalibration")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MachineCalibration([FromQuery] GetMachineCalibrationModel model)
    {
        _logger.LogInformation($"MachineCalibration API request received. ClusterNodeTypeId: {model?.ClusterNodeTypeId}, CalibrationId: '{model?.CalibrationId}', Endpoint: '{model?.Endpoint}', ProjectId: {model?.ProjectId}");
        var validationResult = new ClusterInformationValidator(model).Validate();
        if (!validationResult.IsValid)
        {
            _logger.LogWarning($"MachineCalibration validation failed: {validationResult.Message}");
            throw new InputValidationException(validationResult.Message);
        }

        var result = await _service.GetMachineCalibration(model.ClusterNodeTypeId, model.CalibrationId, model.Endpoint, model.ProjectId, model.SessionCode);
        _logger.LogInformation($"MachineCalibration API request completed. ClusterNodeTypeId: {model.ClusterNodeTypeId}");
        return Content(result, "application/json");
    }

    #endregion
}