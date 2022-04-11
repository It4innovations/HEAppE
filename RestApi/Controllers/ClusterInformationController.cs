using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using HEAppE.ServiceTier.ClusterInformation;
using HEAppE.RestApiModels.ClusterInformation;
using System;
using Microsoft.AspNetCore.Http;
using HEAppE.ExtModels.ClusterInformation.Models;
using Microsoft.Extensions.Logging;
using HEAppE.Utils.Validation;
using HEAppE.RestApi.InputValidator;
using HEAppE.BusinessLogicTier.Logic;

namespace HEAppE.RestApi.Controllers
{
    /// <summary>
    /// Cluster information Endpoint
    /// </summary>
    [ApiController]
    [Route("heappe/[controller]")]
    [Produces("application/json")]
    public class ClusterInformationController : BaseController<ClusterInformationController>
    {
        #region Instances
        private IClusterInformationService _service = new ClusterInformationService();
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger</param>
        public ClusterInformationController(ILogger<ClusterInformationController> logger) : base(logger)
        {

        }
        #endregion
        #region Methods
        /// <summary>
        /// Get available clusters
        /// </summary>
        /// <returns></returns>
        [HttpGet("ListAvailableClusters")]
        [RequestSizeLimit(0)]
        [ProducesResponseType(typeof(IEnumerable<ClusterExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ListAvailableClusters()
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"ClusterInformation\" Method: \"ListAvailableClusters\"");
                return Ok(_service.ListAvailableClusters());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Get command template parameters name
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetCommandTemplateParametersName")]
        [RequestSizeLimit(535)]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCommandTemplateParametersName(GetCommandTemplateParametersNameModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"ClusterInformation\" Method: \"GetCommandTemplateParametersName\"");
                ValidationResult validationResult = new ClusterInformationValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetCommandTemplateParametersName(model.CommandTemplateId, model.UserScriptPath, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Get actual cluster node usage
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("CurrentClusterNodeUsage")]
        [RequestSizeLimit(94)]
        [ProducesResponseType(typeof(ClusterNodeUsageExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult CurrentClusterNodeUsage(CurrentClusterNodeUsageModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"ClusterInformation\" Method: \"CurrentClusterNodeUsage\" Parameters: \"{model}\"");
                ValidationResult validationResult = new ClusterInformationValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetCurrentClusterNodeUsage(model.ClusterNodeId, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        #endregion
    }
}
