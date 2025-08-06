using System;
using System.Collections.Generic;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.JobReporting.Models.DetailedReport;
using HEAppE.ExtModels.JobReporting.Models.ListReport;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.JobReporting;
using HEAppE.ServiceTier.JobReporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HEAppE.RestApi.Controllers;

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
    ///     Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="memoryCache">Memory cache provider</param>
    public JobReportingController(ILogger<JobReportingController> logger, IMemoryCache memoryCache) : base(logger,
        memoryCache)
    {
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Get user groups
    /// </summary>
    /// <param name="sessionCode">Session code</param>
    /// <returns></returns>
    [HttpGet("ListAdaptorUserGroups")]
    [RequestSizeLimit(58)]
    [ProducesResponseType(typeof(UserGroupListReportExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ListAdaptorUserGroups(string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"JobReporting\" Method: \"ListAdaptorUserGroups\" Parameters: SessionCode: \"{sessionCode}\"");
        var validationResult = new SessionCodeValidator(sessionCode).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_service.ListAdaptorUserGroups(sessionCode));
    }

    /// <summary>
    ///     Get resource usage report for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startTime">StartTime</param>
    /// <param name="endTime">EndTime</param>
    /// <param name="subProjects">SubProjects</param>
    /// <param name="sessionCode">SessionCode</param>
    /// <returns></returns>
    [HttpGet("UserResourceUsageReport")]
    [RequestSizeLimit(166)]
    [ProducesResponseType(typeof(IEnumerable<ProjectReportExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult UserResourceUsageReport(long userId, DateTime? startTime, DateTime? endTime,
        [FromQuery] string[] subProjects, string sessionCode)
    {
        var model = new UserResourceUsageReportModel
        {
            StartTime = startTime ?? DateTime.MinValue,
            EndTime = endTime ?? DateTime.UtcNow,
            UserId = userId,
            SessionCode = sessionCode
        };
        _logger.LogDebug($"Endpoint: \"JobReporting\" Method: \"UserResourceUsageReport\" Parameters: \"{model}\"");
        var validationResult = new JobReportingValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_service.UserResourceUsageReport(model.UserId, model.StartTime, model.EndTime, subProjects,
            model.SessionCode));
    }

    /// <summary>
    ///     Get resource usage for user group
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="startTime">StartTime</param>
    /// <param name="endTime">EndTime</param>
    /// <param name="subProjects">SubProjects</param>
    /// <param name="sessionCode">SessionCode</param>
    /// <returns></returns>
    [HttpGet("UserGroupResourceUsageReport")]
    [RequestSizeLimit(168)]
    [ProducesResponseType(typeof(ProjectReportExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult UserGroupResourceUsageReport(long groupId, DateTime? startTime, DateTime? endTime,
        [FromQuery] string[] subProjects, string sessionCode)
    {
        var model = new UserGroupResourceUsageReportModel
        {
            StartTime = startTime ?? DateTime.MinValue,
            EndTime = endTime ?? DateTime.UtcNow,
            GroupId = groupId,
            SessionCode = sessionCode
        };
        _logger.LogDebug(
            $"Endpoint: \"JobReporting\" Method: \"UserGroupResourceUsageReport\" Parameters: \"{model}\"");
        var validationResult = new JobReportingValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_service.UserGroupResourceUsageReport(model.GroupId, model.StartTime, model.EndTime, subProjects,
            model.SessionCode));
    }

    /// <summary>
    ///     Get aggregated resource report usage for user groups
    /// </summary>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    [HttpGet("AggregatedUserGroupResourceUsageReport")]
    [ApiExplorerSettings(GroupName = "DetailedJobReporting")]
    [RequestSizeLimit(168)]
    [ProducesResponseType(typeof(IEnumerable<ProjectAggregatedReportExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult AggregatedUserGroupResourceUsageReport(DateTime? startTime, DateTime? endTime,
        string sessionCode)
    {
        var model = new GetAggredatedUserGroupResourceUsageReportModel
        {
            StartTime = startTime ?? DateTime.MinValue, // Default value is 0001-01-01 00:00:00
            EndTime = endTime ?? DateTime.UtcNow,
            SessionCode = sessionCode
        };
        _logger.LogDebug(
            $"Endpoint: \"JobReporting\" Method: \"AggregatedUserGroupResourceUsageReport\" Parameters: \"{model}\"");
        var validationResult = new JobReportingValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_service.AggregatedUserGroupResourceUsageReport(model.StartTime, model.EndTime, model.SessionCode));
    }

    /// <summary>
    ///     Get resource usage for executed job
    /// </summary>
    /// <param name="sessionCode">Session code</param>
    /// <param name="jobId">Job ID</param>
    /// <returns></returns>
    [HttpGet("ResourceUsageReportForJob")]
    [RequestSizeLimit(86)]
    [ProducesResponseType(typeof(ProjectExtendedReportExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ResourceUsageReportForJob(string sessionCode, long jobId)
    {
        var model = new ResourceUsageReportForJobModel
        {
            SessionCode = sessionCode,
            JobId = jobId
        };
        _logger.LogDebug($"Endpoint: \"JobReporting\" Method: \"ResourceUsageReportForJob\" Parameters: \"{model}\"");
        var validationResult = new JobReportingValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_service.ResourceUsageReportForJob(model.JobId, model.SessionCode));
    }

    /// <summary>
    ///     Get job state aggregation report
    /// </summary>
    /// <param name="sessionCode">Session code</param>
    /// <returns></returns>
    [HttpGet("JobsStateAgregationReport")]
    [RequestSizeLimit(90)]
    [ProducesResponseType(typeof(IEnumerable<JobStateAggregationReportExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult JobAgregationReport(string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"JobReporting\" Method: \"GetJobsStateAgregationReport\" Parameters: SessionCode: \"{sessionCode}\"");
        var validationResult = new SessionCodeValidator(sessionCode).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_service.GetJobsStateAgregationReport(sessionCode));
    }

    /// <summary>
    ///     Get job detailed report
    /// </summary>
    /// <param name="subProjects">SubProjects</param>
    /// <param name="sessionCode">Session code</param>
    /// <returns></returns>
    [HttpGet("JobsDetailedReport")]
    [ApiExplorerSettings(GroupName = "DetailedJobReporting")]
    [RequestSizeLimit(90)]
    [ProducesResponseType(typeof(IEnumerable<ProjectDetailedReportExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult JobsDetailedReport([FromQuery] string[] subProjects, string sessionCode)
    {
        _logger.LogDebug(
            $"Endpoint: \"JobReporting\" Method: \"JobsDetailedReport\" Parameters: SessionCode: \"{sessionCode}\"");
        var validationResult = new SessionCodeValidator(sessionCode).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_service.JobsDetailedReport(subProjects, sessionCode));
    }

    #endregion
}