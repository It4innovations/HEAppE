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
using Microsoft.Extensions.Caching.Memory;
using HEAppE.Utils;

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
        /// <param name="logger">Logger instance</param>
        /// <param name="cacheProvider">Memory cache instance</param>
        public ClusterInformationController(ILogger<ClusterInformationController> logger, IMemoryCache cacheProvider) : base(logger, cacheProvider)
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

                string memoryCacheKey = nameof(ListAvailableClusters);

                if (_cacheProvider.TryGetValue(memoryCacheKey, out object value))
                {
                    _logger.LogInformation($"Using Memory Cache to get value for key: \"{memoryCacheKey}\"");
                    return Ok(value);
                }
                else
                {
                    _logger.LogInformation($"Reloading Memory Cache value for key: \"{memoryCacheKey}\"");
                    object result = _service.ListAvailableClusters();
                    _cacheProvider.Set(memoryCacheKey, result, TimeSpan.FromMinutes(30));
                    return Ok(result);
                }
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

                string memoryCacheKey = StringUtils.CreateIdentifierHash(
                    new List<string>()
                        {   model.SessionCode,
                            nameof(GetCommandTemplateParametersName),
                            model.UserScriptPath.ToString(),
                            model.CommandTemplateId.ToString()
                        }
                    );

                if (_cacheProvider.TryGetValue(memoryCacheKey, out object value))
                {
                    _logger.LogInformation($"Using Memory Cache to get value for key: \"{memoryCacheKey}\"");
                    return Ok(value);
                }
                else
                {
                    _logger.LogInformation($"Reloading Memory Cache value for key: \"{memoryCacheKey}\"");
                    object result = _service.GetCommandTemplateParametersName(model.CommandTemplateId, model.UserScriptPath, model.SessionCode);
                    _cacheProvider.Set(memoryCacheKey, result, TimeSpan.FromMinutes(2));
                    return Ok(result);
                }
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

                string memoryCacheKey = StringUtils.CreateIdentifierHash(
                    new List<string>() 
                        {   model.SessionCode, 
                            nameof(CurrentClusterNodeUsage), 
                            model.ClusterNodeId.ToString() 
                        }
                    );

                if (_cacheProvider.TryGetValue(memoryCacheKey, out object value))
                {
                    _logger.LogInformation($"Using Memory Cache to get value for key: \"{memoryCacheKey}\"");
                    return Ok(value);
                }
                else
                {
                    _logger.LogInformation($"Reloading Memory Cache value for key: \"{memoryCacheKey}\"");
                    object result = _service.GetCurrentClusterNodeUsage(model.ClusterNodeId, model.SessionCode);
                    _cacheProvider.Set(memoryCacheKey, result, TimeSpan.FromMinutes(2));
                    return Ok(result);
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
