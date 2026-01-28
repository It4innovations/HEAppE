using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.UserAndLimitationManagement;
using HEAppE.Services.UserOrg;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.RestApi.Controllers;

/// <summary>
///     User and limitation Endpoint
/// </summary>

[ApiController]
[Route("heappe/[controller]")]
[Produces("application/json")]
public class UserAndLimitationManagementController : BaseController<UserAndLimitationManagementController>
{
    #region Instances

    private readonly IUserAndLimitationManagementService _service;

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="memoryCache">Memory cache provider</param>
    /// <param name="sshCertificateAuthorityService">SSH Certificate Authority Service</param>
    public UserAndLimitationManagementController(ILogger<UserAndLimitationManagementController> logger,
        IMemoryCache memoryCache, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys) : base(logger, memoryCache)
    {
        _service = new UserAndLimitationManagementService(_cacheProvider, userOrgService, sshCertificateAuthorityService, httpContextKeys);
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Provide user authentication via OpenId token.
    /// </summary>
    /// <param name="model">Authentication credentials</param>
    /// <returns></returns>
    [HttpPost("AuthenticateLexisToken")]
    [RequestSizeLimit(2048)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(UnauthorizedResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AuthenticateLexisTokenAsync(AuthenticateLexisTokenModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateLexisToken\" Parameters: \"{model}\"");
        if (JwtTokenIntrospectionConfiguration.IsEnabled || LexisAuthenticationConfiguration.UseBearerAuth)
        {
            _logger.LogInformation("Lexis token authentication is handled by middleware. Returning empty string.");
            return Ok("HEADER-AUTH-NEEDED");
        }
        var validationResult = new UserAndLimitationManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.AuthenticateUserAsync(model.Credentials));
    }

    /// <summary>
    ///     Provide user authentication via OpenId token.
    /// </summary>
    /// <param name="model">Authentication credentials</param>
    /// <returns></returns>
    [HttpPost("AuthenticateUserOpenId")]
    [RequestSizeLimit(4096)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(UnauthorizedResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AuthenticateUserOpenIdAsync(AuthenticateUserOpenIdModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserOpenId\" Parameters: \"{model}\"");
        var validationResult = new UserAndLimitationManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.AuthenticateUserAsync(model.Credentials));
    }

    /// <summary>
    ///     Provide user authentication to OpenStack.
    /// </summary>
    /// <param name="model">Authentication credentials</param>
    /// <returns></returns>
    [HttpPost("AuthenticateUserOpenStack")]
    [RequestSizeLimit(4096)]
    [ProducesResponseType(typeof(OpenStackApplicationCredentialsExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AuthenticateUserOpenStackAsync(AuthenticateUserOpenIdOpenStackModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserOpenStack\" Parameters: \"{model}\"");
        var validationResult = new UserAndLimitationManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.AuthenticateUserToOpenStackAsync(model.Credentials, model.ProjectId));
    }

    /// <summary>
    ///     Provide user authentication
    /// </summary>
    /// <param name="model">Authentication credentials</param>
    /// <returns></returns>
    [HttpPost("AuthenticateUserPassword")]
    [RequestSizeLimit(148)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AuthenticateUserPasswordAsync(AuthenticateUserPasswordModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserPassword\" Parameters: \"{model}\"");
        var validationResult = new UserAndLimitationManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.AuthenticateUserAsync(model.Credentials));
    }

    /// <summary>
    ///     Provide user authentication
    /// </summary>
    /// <param name="model">Authentication credentials</param>
    /// <returns></returns>
    [HttpPost("AuthenticateUserDigitalSignature")]
    [RequestSizeLimit(4000)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AuthenticateUserDigitalSignatureAsync(AuthenticateUserDigitalSignatureModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserDigitalSignature\" Parameters: \"{model}\"");
        var validationResult = new UserAndLimitationManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.AuthenticateUserAsync(model.Credentials));
    }

    /// <summary>
    ///     Get current resource usage
    /// </summary>
    /// <param name="sessionCode">Session code</param>
    /// <returns></returns>
    [HttpGet("CurrentUsageAndLimitationsForCurrentUser")]
    [RequestSizeLimit(60)]
    [ProducesResponseType(typeof(IEnumerable<ProjectResourceUsageExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CurrentUsageAndLimitationsForCurrentUser(string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"UserAndLimitationManagement\" Method: \"GetCurrentUsageAndLimitationsForCurrentUser\" Parameters: \"{sessionCode}\"");
        var validationResult = new SessionCodeValidator(sessionCode).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_service.CurrentUsageAndLimitationsForCurrentUserByProject(sessionCode));
    }

    /// <summary>
    ///     Get projects for current user
    /// </summary>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("ProjectsForCurrentUser")]
    [RequestSizeLimit(60)]
    [ProducesResponseType(typeof(IEnumerable<ProjectReferenceExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ProjectsForCurrentUser(string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"UserAndLimitationManagement\" Method: \"GetProjectsForCurrentUser\" Parameters: \"{sessionCode}\"");
        var validationResult = new SessionCodeValidator(sessionCode).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_service.ProjectsForCurrentUser(sessionCode));
    }

    /// <summary>
    ///     Get current user info
    /// </summary>
    /// <param name="sessionCode">Session code</param>
    /// <returns></returns>
    [HttpGet("CurrentUserInfo")]
    [RequestSizeLimit(60)]
    [ProducesResponseType(typeof(AdaptorUserExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetCurrentUserInfo(string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"UserAndLimitationManagement\" Method: \"CurrentUserInfo\" Parameters: \"{sessionCode}\"");
        var validationResult = new SessionCodeValidator(sessionCode).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_service.GetCurrentUserInfo(sessionCode));
    }

    #endregion
}