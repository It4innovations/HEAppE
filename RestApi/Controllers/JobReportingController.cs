using System;
using HEAppE.ServiceTier.JobReporting;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.RestApiModels.JobReporting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using HEAppE.RestApi.InputValidator;
using HEAppE.Utils.Validation;
using HEAppE.BusinessLogicTier.Logic;
using System.Collections.Generic;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HEAppE.RestApi.Controllers
{
    [ApiController]
    [Route("heappe/[controller]")]
    [Produces("application/json")]
    public class JobReportingController : BaseController<JobReportingController>
    {
        #region Instances
        private readonly IJobReportingService _service = new JobReportingService();
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="memoryCache">Memory cache provider</param>
        public JobReportingController(ILogger<JobReportingController> logger, IMemoryCache memoryCache) : base(logger, memoryCache)
        {

        }
        #endregion
        #region Methods
        /// <summary>
        /// Get user groups
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("ListAdaptorUserGroups")]
        [RequestSizeLimit(58)]
        [ProducesResponseType(typeof(IEnumerable<AdaptorUserGroupExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ListAdaptorUserGroups(string sessionCode)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobReporting\" Method: \"ListAdaptorUserGroups\" Parameters: SessionCode: \"{sessionCode}\"");
                ValidationResult validationResult = new SessionCodeValidator(sessionCode).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.ListAdaptorUserGroups(sessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Get resource usage report for user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("GetUserResourceUsageReport")]
        [RequestSizeLimit(166)]
        [ProducesResponseType(typeof(UserResourceUsageReportExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetUserResourceUsageReport(GetUserResourceUsageReportModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobReporting\" Method: \"GetUserResourceUsageReport\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobReportingValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetUserResourceUsageReport(model.UserId, model.StartTime, model.EndTime, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Get resource usage for user group
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("GetUserGroupResourceUsageReport")]
        [RequestSizeLimit(168)]
        [ProducesResponseType(typeof(UserGroupResourceUsageReportExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetUserGroupResourceUsageReport(GetUserGroupResourceUsageReportModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobReporting\" Method: \"GetUserGroupResourceUsageReport\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobReportingValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetUserGroupResourceUsageReport(model.GroupId, model.StartTime, model.EndTime, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Get resource usage for executed job
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("GetResourceUsageReportForJob")]
        [RequestSizeLimit(86)]
        [ProducesResponseType(typeof(SubmittedJobInfoUsageReportExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetResourceUsageReportForJob(GetResourceUsageReportForJobModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobReporting\" Method: \"GetResourceUsageReportForJob\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobReportingValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetResourceUsageReportForJob(model.JobId, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Get job state aggregation report
        /// </summary>
        /// <param name="sessionCode">Session code</param>
        /// <returns></returns>
        [HttpGet("GetJobsStateAgregationReport")]
        [RequestSizeLimit(90)]
        [ProducesResponseType(typeof(IEnumerable<JobStateAggregationReportExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetJobAgregationReport(string sessionCode)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobReporting\" Method: \"GetJobsStateAgregationReport\" Parameters: SessionCode: \"{sessionCode}\"");
                ValidationResult validationResult = new SessionCodeValidator(sessionCode).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetJobsStateAgregationReport(sessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Get job detailed report
        /// </summary>
        /// <param name="sessionCode">Session code</param>
        /// <returns></returns>
        [HttpGet("GetJobsDetailedReport")]
        [RequestSizeLimit(90)]
        [ProducesResponseType(typeof(IEnumerable<SubmittedJobInfoReportExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetJobsDetailedReport(string sessionCode)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobReporting\" Method: \"GetJobsDetailedReport\" Parameters: SessionCode: \"{sessionCode}\"");
                ValidationResult validationResult = new SessionCodeValidator(sessionCode).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetJobsDetailedReport(sessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        #endregion
    }
}
