using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.ClusterInformation;
using HEAppE.ServiceTier.ClusterInformation;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.Utils.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

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
        private IClusterInformationService _service;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="cacheProvider">Memory cache instance</param>
        public ClusterInformationController(ILogger<ClusterInformationController> logger, IMemoryCache cacheProvider) : base(logger, cacheProvider)
        {
            _service = new ClusterInformationService(cacheProvider);
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
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult ListAvailableClusters(string clusterName = null, string nodeTypeName = null, string projectName = null, string accountingString = null, string commandTemplateName = null)
        {
            _logger.LogDebug($"Endpoint: \"ClusterInformation\" Method: \"ListAvailableClusters\"");
            return Ok(_service.ListAvailableClusters(clusterName, nodeTypeName, projectName, accountingString, commandTemplateName));
        }

        /// <summary>
        /// Get command template parameters name
        /// </summary>
        /// <returns></returns>
        [HttpPost("RequestCommandTemplateParametersName")]
        [RequestSizeLimit(535)]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult RequestCommandTemplateParametersName(GetCommandTemplateParametersNameModel model)
        {
            _logger.LogDebug($"Endpoint: \"ClusterInformation\" Method: \"GetCommandTemplateParametersName\"");
            ValidationResult validationResult = new ClusterInformationValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_service.RequestCommandTemplateParametersName(model.CommandTemplateId, model.ProjectId, model.UserScriptPath, model.SessionCode));
        }

        /// <summary>
        /// Get actual cluster node usage
        /// </summary>
        /// <param name="sessionCode">Session code</param>
        /// <param name="clusterNodeId">ClusterNode ID</param>
        /// <param name="projectId">Project ID</param>
        /// <returns></returns>
        [HttpGet("CurrentClusterNodeUsage")]
        [RequestSizeLimit(154)]
        [ProducesResponseType(typeof(ClusterNodeUsageExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult CurrentClusterNodeUsage(string sessionCode, long clusterNodeId, long projectId)
        {
            var model = new CurrentClusterNodeUsageModel()
            {
                SessionCode = sessionCode,
                ClusterNodeId = clusterNodeId,
                ProjectId = projectId
            };
            _logger.LogDebug($"Endpoint: \"ClusterInformation\" Method: \"CurrentClusterNodeUsage\" Parameters: \"{model}\"");
            ValidationResult validationResult = new ClusterInformationValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_service.GetCurrentClusterNodeUsage(model.ClusterNodeId, model.ProjectId, model.SessionCode));
        }
        #endregion
    }
}
