using System;
using HEAppE.ServiceTier.JobReporting;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.RestApiModels.JobReporting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        public JobReportingController(ILogger<JobReportingController> logger) : base(logger)
        {

        }
        #endregion
        #region Methods
        ///// <summary>
        ///// Get user groups
        ///// </summary>
        ///// <param name="model"></param>
        ///// <returns></returns>
        ///// TODO must be solved HEAppE UserRoles
        //[HttpPost("ListAdaptorUserGroups")]
        //[RequestSizeLimit(54)]
        //[ProducesResponseType(typeof(IEnumerable<AdaptorUserGroupExt>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        //[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public IActionResult ListAdaptorUserGroups(ListAdaptorUserGroupsView model)
        //{
        //    try
        //    {
        //        _logger.LogDebug($"Endpoint: \"JobReporting\" Method: \"ListAdaptorUserGroups\" Parameters: \"{model}\"");
        //        return Ok(_service.ListAdaptorUserGroups(model.SessionCode));
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(e.Message);
        //    }
        //}

        /// <summary>
        /// Get resource usage report for user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("GetUserResourceUsageReport")]
        [RequestSizeLimit(158)]
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
        [RequestSizeLimit(158)]
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
        [RequestSizeLimit(80)]
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
                return Ok(_service.GetResourceUsageReportForJob(model.JobId, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        #endregion
    }
}
