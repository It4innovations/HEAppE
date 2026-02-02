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
    /// <param name="sshCertificateAuthorityService">SSH Certificate Authority service</param>
    public ClusterInformationController(ILogger<ClusterInformationController> logger, IMemoryCache cacheProvider, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys) :
        base(logger, cacheProvider)
    {
        _service = new ClusterInformationService(cacheProvider, userOrgService, sshCertificateAuthorityService, httpContextKeys);
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
        _logger.LogDebug($"Endpoint: \"ClusterInformation\" Method: \"ListAvailableClusters\", Parameters: \"SessionCode: {sessionCode}, ClusterName: {clusterName}, NodeTypeName: {nodeTypeName}, " +
                         $"ProjectName: {projectName}, AccountingString: {accountingString}, CommandTemplateName: {commandTemplateName}\"");
        ListAvailableClustersModel model = new()
        {
            SessionCode = sessionCode
        };
        var validationResult = new ClusterInformationValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);
        return Ok(await _service.ListAvailableClusters(sessionCode, clusterName, nodeTypeName, projectName, accountingString,
            commandTemplateName, forceRefresh ?? false));
    }

    [HttpPost("ListAvailableClustersClearCache")]
    [RequestSizeLimit(200)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ListAvailableClustersClearCache(string sessionCode)
    {
        ListAvailableClustersModel model = new()
        {
            SessionCode = sessionCode
        };
        var validationResult = new ClusterInformationValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);
        return Ok(_service.ListAvailableClustersClearCache(sessionCode));
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
        _logger.LogDebug("Endpoint: \"ClusterInformation\" Method: \"GetCommandTemplateParametersName\"");
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
        _logger.LogDebug(
            $"Endpoint: \"ClusterInformation\" Method: \"CurrentClusterNodeUsage\" Parameters: \"{model}\"");
        var validationResult = new ClusterInformationValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.GetCurrentClusterNodeUsage(model.ClusterNodeId, model.ProjectId, model.SessionCode));
    }

    #endregion
}