﻿using System;
using System.Collections.Generic;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.RestApiModels.UserAndLimitationManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        [RequestSizeLimit(15_000)]
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
                return Ok(_service.AuthenticateUser(model.Credentials));
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
        [RequestSizeLimit(15_000)]
        [ProducesResponseType(typeof(OpenStackApplicationCredentialsExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult AuthenticateUserOpenStack(AuthenticateUserOpenIdModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserOpenStack\" Parameters: \"{model}\"");
                return Ok(_service.AuthenticateUserToOpenStack(model.Credentials));
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
        [RequestSizeLimit(220)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult AuthenticateUserPassword(AuthenticateUserPasswordModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserPassword\" Parameters: \"{model}\"");
                return Ok(_service.AuthenticateUser(model.Credentials));
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
        public IActionResult AuthenticateUserDigitalSignature(AuthenticateUserDigitalSignatureModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"UserAndLimitationManagement\" Method: \"AuthenticateUserDigitalSignature\" Parameters: \"{model}\"");
                return Ok(_service.AuthenticateUser(model.Credentials));
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
        [RequestSizeLimit(54)]
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