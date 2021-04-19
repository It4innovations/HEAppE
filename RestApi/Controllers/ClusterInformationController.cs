﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using HEAppE.ServiceTier.ClusterInformation;
using HEAppE.RestApiModels.ClusterInformation;
using System;
using Microsoft.AspNetCore.Http;
using HEAppE.ExtModels.ClusterInformation.Models;
using Microsoft.Extensions.Logging;

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
        /// Get actual cluster node usage
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("CurrentClusterNodeUsage")]
        [RequestSizeLimit(80)]
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