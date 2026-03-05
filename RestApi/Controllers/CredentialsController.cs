using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.Management.Models;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.Management;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using HEAppE.ServiceTier.Management;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.RestApi.Controllers;

[ApiController]
[Route("heappe/[controller]")]
public class CredentialsController : ControllerBase
{
    #region Instances

    private readonly IManagementService _managementService;

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="memoryCache">Memory cache provider</param>
    public CredentialsController(ILogger<ManagementController> logger, IMemoryCache memoryCache, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys, IExpirioService expirioService) : base()
    {
        _managementService = new ManagementService(userOrgService, sshCertificateAuthorityService, httpContextKeys, expirioService);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates a new credential. Fill only the properties relevant to the selected AuthType.
    /// </summary>
    [HttpPost("CreateCredential")]
    [RequestSizeLimit(2000)]
    [ProducesResponseType(typeof(CredentialResponseExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]

    public async Task<IActionResult> CreateCredential([FromBody] CreateCredentialModel model)
    {
        //TODO: use logger?
        //_logger.LogDebug("Endpoint: \"Management\" Method: \"GenerateSecureShellKey\"");

        // Validation is crucial here to ensure only the right fields are provided for the given AuthType
        var validationResult = new CredentialValidator(model).Validate(); 
        if (!validationResult.IsValid) 
            throw new InputValidationException(validationResult.Message);

        var result = await _managementService.CreateCredentialAsync(model.ProjectId, model.SessionCode, model.Username, model.AuthType, 
                                                                    model.GenerateNewKey, model.ProvidedPrivateKey, model.Password, model.Passphrase);
        return Ok(result);
    }

    /// <summary>
    /// Gets a list of credentials for a project.
    /// </summary>
    [HttpGet("GetCredentials")]
    [RequestSizeLimit(2000)]
     [ProducesResponseType(typeof(List<CredentialResponseExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
      public async Task<IActionResult> GetCredentials(long projectId, [FromQuery] string sessionCode)
    {
        /* TODO: use logger?
        _logger.LogDebug(
            $"Endpoint: \"Management\" Method: \"GetCredentials\" Parameters: ProjectId: \"{projectId}\", SessionCode: \"{sessionCode}\"");*/
        CreateCredentialModel model = new()
        {
            ProjectId = projectId,
            SessionCode = sessionCode
        };
        var validationResult = new CredentialsValidator(model).Validate();
        if (!validationResult.IsValid) 
            throw new InputValidationException(validationResult.Message);

        var result = await _managementService.GetCredentialsAsync(model.ProjectId, model.SessionCode);
        return Ok(result);
    }

    /// <summary>
    /// Modifies credentials for a project.
    /// </summary>
    [HttpGet("ModifyCredential")]
    [RequestSizeLimit(2000)]
    [ProducesResponseType(typeof(List<CredentialResponseExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ModifyCredential(CreateCredentialModel model)
    {
        //TODO: use logger?
        //_logger.LogDebug("Endpoint: \"Management\" Method: \"ModifyCredential\"");
        var validationResult = new CredentialsValidator(model).Validate();
        if (!validationResult.IsValid) 
            throw new InputValidationException(validationResult.Message);

        var result = await _managementService.ModifyCredentialAsync(model.ProjectId, model.SessionCode, model.Username, model.AuthType);
        return Ok(result);
    }

    /// <summary>
    ///     Remove credential for a project.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("RemoveCredential")]
    [RequestSizeLimit(2000)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveCredential(CreateCredentialModel model)
    {
        //_logger.LogDebug("Endpoint: \"Management\" Method: \"RemoveCredential\"");
        var validationResult = new CredentialValidator(model).Validate();
        if (!validationResult.IsValid) 
            throw new InputValidationException(validationResult.Message);

        await _managementService.RemoveCredential(model.ProjectId, model.SessionCode, model.Username);
        return Ok("Credential removed");
    }

    #endregion

}
