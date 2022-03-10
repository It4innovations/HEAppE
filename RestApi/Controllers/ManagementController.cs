using HEAppE.BusinessLogicTier.Logic;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.Management;
using HEAppE.ServiceTier.Management;
using HEAppE.Utils.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private IManagementService _service = new ManagementService();
        #endregion
        #region Constructors
        public ManagementController(ILogger<ManagementController> logger) : base(logger)
        {
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

                return Ok(_service.CreateCommandTemplate(model.GenericCommandTemplateId, model.Name,model.Description,model.Code,
                                                         model.ExecutableFile, model.PreparationScript,model.SessionCode));
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

                return Ok(_service.ModifyCommandTemplate(model.CommandTemplateId, model.Name, model.Description, model.Code,
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
        [RequestSizeLimit(84)]
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

                return Ok(_service.RemoveCommandTemplate(model.CommandTemplateId, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        #endregion
    }
}
