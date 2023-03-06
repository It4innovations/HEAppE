using System;
using System.Collections.Generic;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.RestApiModels.UserAndLimitationManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using HEAppE.Utils.Validation;
using HEAppE.RestApi.InputValidator;
using HEAppE.BusinessLogicTier.Logic;
using System.Threading.Tasks;
using HEAppE.Utils;
using Microsoft.Extensions.Caching.Memory;
using HEAppE.OpenStackAPI.Configuration;

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
        [HttpPost("AuthenticateUserOpenId")]
        [RequestSizeLimit(2048)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> AuthenticateUserOpenIdAsync(AuthenticateUserOpenIdModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserOpenId\" Parameters: \"{model}\"");
                ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(await _service.AuthenticateUserAsync(model.Credentials));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        /// <summary>
        /// Provide user authentication to OpenStack.
        /// </summary>
        /// <param name="model">Authentication credentials</param>
        /// <returns></returns>
        [HttpPost("AuthenticateUserOpenStack")]
        [RequestSizeLimit(2088)]
        [ProducesResponseType(typeof(OpenStackApplicationCredentialsExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> AuthenticateUserOpenStackAsync(AuthenticateUserOpenIdOpenStackModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserOpenStack\" Parameters: \"{model}\"");
                ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(await _service.AuthenticateUserToOpenStackAsync(model.Credentials, model.ProjectId));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
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
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserPassword\" Parameters: \"{model}\"");
                ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(await _service.AuthenticateUserAsync(model.Credentials));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
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
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserDigitalSignature\" Parameters: \"{model}\"");
                ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(await _service.AuthenticateUserAsync(model.Credentials));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        /// <summary>
        /// Get current resource usage
        /// </summary>
        /// <param name="model">Session code</param>
        /// <returns></returns>
        [HttpPost("GetCurrentUsageAndLimitationsForCurrentUser")]
        [RequestSizeLimit(60)]
        [ProducesResponseType(typeof(IEnumerable<ResourceUsageExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [Obsolete]
        public IActionResult Obsolete_GetCurrentUsageAndLimitationsForCurrentUser(GetCurrentUsageAndLimitationsForCurrentUserModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"GetCurrentUsageAndLimitationsForCurrentUser\" Parameters: \"{model}\"");
                ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetCurrentUsageAndLimitationsForCurrentUser(model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        /// <summary>
        /// Get current resource usage
        /// </summary>
        /// <param name="model">Session code</param>
        /// <returns></returns>
        [HttpGet("GetCurrentUsageAndLimitationsForCurrentUser")]
        [RequestSizeLimit(60)]
        [ProducesResponseType(typeof(IEnumerable<ResourceUsageExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult GetCurrentUsageAndLimitationsForCurrentUser(string sessionCode)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"GetCurrentUsageAndLimitationsForCurrentUser\" Parameters: \"{sessionCode}\"");
                ValidationResult validationResult = new SessionCodeValidator(sessionCode).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetCurrentUsageAndLimitationsForCurrentUser(sessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        [HttpGet("GetProjectsForCurrentUser")]
        [RequestSizeLimit(60)]
        [ProducesResponseType(typeof(IEnumerable<ProjectReferenceExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult GetProjectsForCurrentUser(string sessionCode)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"GetProjectsForCurrentUser\" Parameters: \"{sessionCode}\"");
                ValidationResult validationResult = new SessionCodeValidator(sessionCode).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetProjectsForCurrentUser(sessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }
        #endregion
    }
}