using HEAppE.BusinessLogicTier.Logic;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.JobReporting.Models.DetailedReport;
using HEAppE.ExtModels.JobReporting.Models.ListReport;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.JobReporting;
using HEAppE.ServiceTier.JobReporting;
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
        /// <param name="sessionCode">Session code</param>
        /// <returns></returns>
        [HttpGet("ListAdaptorUserGroups")]
        [RequestSizeLimit(58)]
        [ProducesResponseType(typeof(UserGroupListReportExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
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
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        /// <summary>
        /// Get resource usage report for user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="startTime">StartTime</param>
        /// <param name="endTime">EndTime</param>
        /// <param name="sessionCode">SessionCode</param>
        /// <returns></returns>
        [HttpGet("UserResourceUsageReport")]
        [RequestSizeLimit(166)]
        [ProducesResponseType(typeof(IEnumerable<UserGroupReportExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult UserResourceUsageReport(long userId, DateTime startTime, DateTime endTime, string sessionCode)
        {
            try
            {
                var model = new UserResourceUsageReportModel()
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    UserId = userId,
                    SessionCode = sessionCode
                };
                _logger.LogDebug($"Endpoint: \"JobReporting\" Method: \"UserResourceUsageReport\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobReportingValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.UserResourceUsageReport(model.UserId, model.StartTime, model.EndTime, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        /// <summary>
        /// Get resource usage for user group
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="startTime">StartTime</param>
        /// <param name="endTime">EndTime</param>
        /// <param name="sessionCode">SessionCode</param>
        /// <returns></returns>
        [HttpGet("UserGroupResourceUsageReport")]
        [RequestSizeLimit(168)]
        [ProducesResponseType(typeof(UserGroupReportExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime, string sessionCode)
        {
            try
            {
                var model = new UserGroupResourceUsageReportModel()
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    GroupId = groupId,
                    SessionCode = sessionCode
                };
                _logger.LogDebug($"Endpoint: \"JobReporting\" Method: \"UserGroupResourceUsageReport\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobReportingValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.UserGroupResourceUsageReport(model.GroupId, model.StartTime, model.EndTime, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        /// <summary>
        /// Get aggregated resource report usage for user groups
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="sessionCode"></param>
        /// <returns></returns>
        [HttpGet("AggregatedUserGroupResourceUsageReport")]
        [RequestSizeLimit(168)]
        [ProducesResponseType(typeof(IEnumerable<UserGroupReportExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult AggregatedUserGroupResourceUsageReport(DateTime startTime, DateTime endTime, string sessionCode)
        {
            try
            {
                var model = new GetAggredatedUserGroupResourceUsageReportModel()
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    SessionCode = sessionCode
                };
                _logger.LogDebug($"Endpoint: \"JobReporting\" Method: \"AggregatedUserGroupResourceUsageReport\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobReportingValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.AggregatedUserGroupResourceUsageReport(model.StartTime, model.EndTime, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        /// <summary>
        /// Get resource usage for executed job
        /// </summary>
        /// <param name="sessionCode">Session code</param>
        /// <param name="jobId">Job ID</param>
        /// <returns></returns>
        [HttpGet("ResourceUsageReportForJob")]
        [RequestSizeLimit(86)]
        [ProducesResponseType(typeof(ProjectReportExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult ResourceUsageReportForJob(string sessionCode, long jobId)
        {
            try
            {
                var model = new ResourceUsageReportForJobModel()
                {
                    SessionCode = sessionCode,
                    JobId = jobId
                };
                _logger.LogDebug($"Endpoint: \"JobReporting\" Method: \"ResourceUsageReportForJob\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobReportingValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.ResourceUsageReportForJob(model.JobId, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        /// <summary>
        /// Get job state aggregation report
        /// </summary>
        /// <param name="sessionCode">Session code</param>
        /// <returns></returns>
        [HttpGet("JobsStateAgregationReport")]
        [RequestSizeLimit(90)]
        [ProducesResponseType(typeof(IEnumerable<JobStateAggregationReportExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult JobAgregationReport(string sessionCode)
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
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        /// <summary>
        /// Get job detailed report
        /// </summary>
        /// <param name="sessionCode">Session code</param>
        /// <returns></returns>
        [HttpGet("JobsDetailedReport")]
        [RequestSizeLimit(90)]
        [ProducesResponseType(typeof(IEnumerable<UserGroupDetailedReportExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult JobsDetailedReport(string sessionCode)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobReporting\" Method: \"JobsDetailedReport\" Parameters: SessionCode: \"{sessionCode}\"");
                ValidationResult validationResult = new SessionCodeValidator(sessionCode).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.JobsDetailedReport(sessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }
        #endregion
    }
}
