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
        private IUserAndLimitationManagementService _service = new UserAndLimitationManagementService();
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger</param>
        public UserAndLimitationManagementController(ILogger<UserAndLimitationManagementController> logger) : base(logger)
        {

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
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult AuthenticateUserOpenId(AuthenticateUserOpenIdModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserOpenId\" Parameters: \"{model}\"");
                ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                return Ok(_service.AuthenticateUserAsync(model.Credentials));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Provide user authentication to OpenStack.
        /// </summary>
        /// <param name="model">Authentication credentials</param>
        /// <returns></returns>
        [HttpPost("AuthenticateUserOpenStack")]
        [RequestSizeLimit(2048)]
        [ProducesResponseType(typeof(OpenStackApplicationCredentialsExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AuthenticateUserOpenStackAsync(AuthenticateUserOpenIdModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserOpenStack\" Parameters: \"{model}\"");
                ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                return Ok(await _service.AuthenticateUserToOpenStackAsync(model.Credentials));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
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
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AuthenticateUserPasswordAsync(AuthenticateUserPasswordModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserPassword\" Parameters: \"{model}\"");
                ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                return Ok(await _service.AuthenticateUserAsync(model.Credentials));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
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
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AuthenticateUserDigitalSignatureAsync(AuthenticateUserDigitalSignatureModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserDigitalSignature\" Parameters: \"{model}\"");
                ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                return Ok(await _service.AuthenticateUserAsync(model.Credentials));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Get current resource usage
        /// </summary>
        /// <param name="model">Session code</param>
        /// <returns></returns>
        [HttpPost("GetCurrentUsageAndLimitationsForCurrentUser")]
        [RequestSizeLimit(56)]
        [ProducesResponseType(typeof(IEnumerable<ResourceUsageExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCurrentUsageAndLimitationsForCurrentUser(GetCurrentUsageAndLimitationsForCurrentUserModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"GetCurrentUsageAndLimitationsForCurrentUser\" Parameters: \"{model}\"");
                ValidationResult validationResult = new UserAndLimitationManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                return Ok(_service.GetCurrentUsageAndLimitationsForCurrentUser(model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        #endregion
    }
}