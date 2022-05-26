using HEAppE.BusinessLogicTier.Logic;
using HEAppE.ExtModels.Management.Models;
using HEAppE.RestApi.Configuration;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.Management;
using HEAppE.ServiceTier.Management;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.Utils;
using HEAppE.Utils.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace HEAppE.RestApi.Controllers
{
    [ApiController]
    [Route("heappe/[controller]")]
    [Produces("application/json")]
    public class ManagementController : BaseController<ManagementController>
    {
        #region Instances
        private readonly IManagementService _managementService;
        private readonly IUserAndLimitationManagementService _userAndManagementService;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="memoryCache">Memory cache provider</param>
        public ManagementController(ILogger<ManagementController> logger, IMemoryCache memoryCache) : base(logger, memoryCache)
        {
            _managementService = new ManagementService();
            _userAndManagementService = new UserAndLimitationManagementService(memoryCache);
        }
        #endregion
        #region Methods
        /// <summary>
        /// Create Command Template from Generic Command Template
        /// </summary>
        /// <param name="model">CreateCommandTemplate</param>
        /// <returns></returns>
        [HttpPost("CreateCommandTemplate")]
        [RequestSizeLimit(1520)]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult CreateCommandTemplate(CreateCommandTemplateModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"CreateCommandTemplate\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                string memoryCacheKey = nameof(ClusterInformationController.ListAvailableClusters);
                _cacheProvider.RemoveKeyFromCache(_logger, memoryCacheKey, nameof(CreateCommandTemplate));

                return Ok(_managementService.CreateCommandTemplate(model.GenericCommandTemplateId, model.Name, model.Description, model.Code,
                                                         model.ExecutableFile, model.PreparationScript, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Modify command template
        /// </summary>
        /// <param name="model">ModifyCommandTemplateModel</param>
        /// <returns></returns>
        [HttpPost("ModifyCommandTemplate")]
        [RequestSizeLimit(1520)]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ModifyCommandTemplate(ModifyCommandTemplateModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"ModifyCommandTemplate\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                string memoryCacheKey = nameof(ClusterInformationController.ListAvailableClusters);
                _cacheProvider.RemoveKeyFromCache(_logger, memoryCacheKey, nameof(ModifyCommandTemplate));

                return Ok(_managementService.ModifyCommandTemplate(model.CommandTemplateId, model.Name, model.Description, model.Code,
                                                         model.ExecutableFile, model.PreparationScript, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Remove command template
        /// </summary>
        /// <param name="model">RemoveCommandTemplateModel</param>
        /// <returns></returns>
        [HttpPost("RemoveCommandTemplate")]
        [RequestSizeLimit(90)]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult RemoveCommandTemplate(RemoveCommandTemplateModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"RemoveCommandTemplate\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                string memoryCacheKey = nameof(ClusterInformationController.ListAvailableClusters);
                _cacheProvider.RemoveKeyFromCache(_logger, memoryCacheKey, nameof(RemoveCommandTemplate));

                return Ok(_managementService.RemoveCommandTemplate(model.CommandTemplateId, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Get HEAppE Infromation
        /// </summary>
        /// <param name="sessionCode">SessionCode</param>
        /// <returns></returns>
        [HttpGet("GetInstanceInformations")]
        [RequestSizeLimit(90)]
        [ProducesResponseType(typeof(InstanceInformationExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetInstanceInformations(string sessionCode)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"GetInstanceInformations\" Parameters: SessionCode: \"{sessionCode}\"");
                ValidationResult validationResult = new SessionCodeValidator(sessionCode).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                var result = _userAndManagementService.ValidateUserPermissions(sessionCode);
                if(result)
                {
                    return Ok(new InstanceInformationExt()
                    {
                        InstanceName = ApplicationAPIConfiguration.InstanceName,
                        Version = ApplicationAPIConfiguration.Version,
                        DeployedIPAddress = ApplicationAPIConfiguration.DeployedIPAddress
                    });
                }
                else
                {
                    return BadRequest(null);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        #endregion
    }
}
