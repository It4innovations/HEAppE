using HEAppE.BusinessLogicTier.Logic;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.Management;
using HEAppE.ServiceTier.ClusterInformation;
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
        private IClusterInformationService _service = new ClusterInformationService();
        #endregion
        #region Constructors
        public ManagementController(ILogger<ManagementController> logger) : base(logger)
        {
        }
        #endregion

        /// <summary>
        /// Creates Command Template from Generic Command Template
        /// </summary>
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
            //TODO (konvicka): Add role checking to Admin
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"CreateCommandTemplate\"");
                ValidationResult validationResult = new ClusterInformationValidator(model).Validate();
                if (!validationResult.IsValid)
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                return Ok(_service.CreateCommandTemplate(
                                                            model.GenericCommandTemplateId,
                                                            model.Name,
                                                            model.Description,
                                                            model.Code,
                                                            model.ExecutableFile,
                                                            model.PreparationScript,
                                                            model.SessionCode
                        ));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        /// Removes Command Template from repository
        /// </summary>
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
            //TODO (konvicka): Add role checking to Admin
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"RemoveCommandTemplate\"");
                ValidationResult validationResult = new ClusterInformationValidator(model).Validate();
                if (!validationResult.IsValid)
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                return Ok(_service.RemoveCommandTemplate(
                                                            model.CommandTemplateId,
                                                            model.SessionCode
                        ));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        /// Modifies Command Template
        /// </summary>
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
            //TODO (konvicka): Add role checking to Admin
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"ModifyCommandTemplate\"");
                ValidationResult validationResult = new ClusterInformationValidator(model).Validate();
                if (!validationResult.IsValid)
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                return Ok(_service.ModifyCommandTemplate(
                                                            model.CommandTemplateId,
                                                            model.Name,
                                                            model.Description,
                                                            model.Code,
                                                            model.ExecutableFile,
                                                            model.PreparationScript,
                                                            model.SessionCode
                        ));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
