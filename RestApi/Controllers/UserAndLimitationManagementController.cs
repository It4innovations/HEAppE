using HEAppE.Exceptions.External;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.UserAndLimitationManagement;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.Utils.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HEAppE.RestApi.Controllers
{
    /// <summary>
    /// User and limitation Endpoint
    /// </summary>
    [ApiController]
    [Route("heappe/[controller]")]
    [Produces("application/json")]
    public class UserAndLimitationManagementController : BaseController<UserAndLimitationManagementController>
    {
        #region Instances
        private IUserAndLimitationManagementService _service;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="memoryCache">Memory cache provider</param>
        public UserAndLimitationManagementController(ILogger<UserAndLimitationManagementController> logger, IMemoryCache memoryCache) : base(logger, memoryCache)
        {
            _service = new UserAndLimitationManagementService(_cacheProvider);
        }
        #endregion
        #region Methods
        /// <summary>
        /// Provide user authentication via OpenId token.
        /// </summary>
        /// <param name="model">Authentication credentials</param>
        /// <returns></returns>
        [HttpPost("AuthenticateLexisToken")]
        [RequestSizeLimit(2048)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(UnauthorizedResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AuthenticateLexisTokenAsync(AuthenticateLexisTokenModel model)
        {
            _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateLexisToken\" Parameters: \"{model}\"");
            ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(await _service.AuthenticateUserAsync(model.Credentials));
        }

        /// <summary>
        /// Provide user authentication via OpenId token.
        /// </summary>
        /// <param name="model">Authentication credentials</param>
        /// <returns></returns>
        [HttpPost("AuthenticateUserOpenId")]
        [RequestSizeLimit(4096)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(UnauthorizedResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AuthenticateUserOpenIdAsync(AuthenticateUserOpenIdModel model)
        {
            _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserOpenId\" Parameters: \"{model}\"");
            ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(await _service.AuthenticateUserAsync(model.Credentials));
        }

        /// <summary>
        /// Provide user authentication to OpenStack.
        /// </summary>
        /// <param name="model">Authentication credentials</param>
        /// <returns></returns>
        [HttpPost("AuthenticateUserOpenStack")]
        [RequestSizeLimit(4096)]
        [ProducesResponseType(typeof(OpenStackApplicationCredentialsExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> AuthenticateUserOpenStackAsync(AuthenticateUserOpenIdOpenStackModel model)
        {
            _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserOpenStack\" Parameters: \"{model}\"");
            ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(await _service.AuthenticateUserToOpenStackAsync(model.Credentials, model.ProjectId));
        }

        /// <summary>
        /// Provide user authentication
        /// </summary>
        /// <param name="model">Authentication credentials</param>
        /// <returns></returns>
        [HttpPost("AuthenticateUserPassword")]
        [RequestSizeLimit(148)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> AuthenticateUserPasswordAsync(AuthenticateUserPasswordModel model)
        {
            _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserPassword\" Parameters: \"{model}\"");
            ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(await _service.AuthenticateUserAsync(model.Credentials));
        }

        /// <summary>
        /// Provide user authentication
        /// </summary>
        /// <param name="model">Authentication credentials</param>
        /// <returns></returns>
        [HttpPost("AuthenticateUserDigitalSignature")]
        [RequestSizeLimit(4000)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> AuthenticateUserDigitalSignatureAsync(AuthenticateUserDigitalSignatureModel model)
        {
            _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserDigitalSignature\" Parameters: \"{model}\"");
            ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(await _service.AuthenticateUserAsync(model.Credentials));
        }

        /// <summary>
        /// Get current resource usage
        /// </summary>
        /// <param name="sessionCode">Session code</param>
        /// <returns></returns>
        [HttpGet("CurrentUsageAndLimitationsForCurrentUser")]
        [RequestSizeLimit(60)]
        [ProducesResponseType(typeof(IEnumerable<ProjectResourceUsageExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult CurrentUsageAndLimitationsForCurrentUser(string sessionCode)
        {
            _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"GetCurrentUsageAndLimitationsForCurrentUser\" Parameters: \"{sessionCode}\"");
            ValidationResult validationResult = new SessionCodeValidator(sessionCode).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_service.CurrentUsageAndLimitationsForCurrentUserByProject(sessionCode));
        }

        /// <summary>
        /// Get projects for current user
        /// </summary>
        /// <param name="sessionCode"></param>
        /// <returns></returns>
        [HttpGet("ProjectsForCurrentUser")]
        [RequestSizeLimit(60)]
        [ProducesResponseType(typeof(IEnumerable<ProjectReferenceExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult ProjectsForCurrentUser(string sessionCode)
        {
            _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"GetProjectsForCurrentUser\" Parameters: \"{sessionCode}\"");
            ValidationResult validationResult = new SessionCodeValidator(sessionCode).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_service.ProjectsForCurrentUser(sessionCode));
        }
        #endregion
    }
}