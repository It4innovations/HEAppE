﻿using HEAppE.Exceptions.External;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.JobManagement;
using HEAppE.ServiceTier.JobManagement;
using HEAppE.Utils.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace HEAppE.RestApi.Controllers
{
    [ApiController]
    [Route("heappe/[controller]")]
    [Produces("application/json")]
    public class JobManagementController : BaseController<JobManagementController>
    {
        #region Instances
        private IJobManagementService _service = new JobManagementService();
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="memoryCache">Memory cache provider</param>
        public JobManagementController(ILogger<JobManagementController> logger, IMemoryCache memoryCache) : base(logger, memoryCache)
        {

        }
        #endregion
        #region Methods
        /// <summary>
        /// Create job specification
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("CreateJob")]
        [RequestSizeLimit(50000)]
        [ProducesResponseType(typeof(SubmittedJobInfoExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult CreateJob(CreateJobByProjectModel model)
        {
            _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"CreateJob\" Parameters: \"{model}\"");
            ValidationResult validationResult = new JobManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_service.CreateJob(model.JobSpecification, model.SessionCode));
        }

        /// <summary>
        /// Submit job
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("SubmitJob")]
        [RequestSizeLimit(94)]
        [ProducesResponseType(typeof(SubmittedJobInfoExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult SubmitJob(SubmitJobModel model)
        {
            _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"SubmitJob\" Parameters: \"{model}\"");
            ValidationResult validationResult = new JobManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_service.SubmitJob(model.CreatedJobInfoId, model.SessionCode));
        }

        /// <summary>
        /// Cancel job
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("CancelJob")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(SubmittedJobInfoExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult CancelJob(CancelJobModel model)
        {
            _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"CancelJob\" Parameters: \"{model}\"");
            ValidationResult validationResult = new JobManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_service.CancelJob(model.SubmittedJobInfoId, model.SessionCode));
        }

        /// <summary>
        /// Delete job
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpDelete("DeleteJob")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult DeleteJob(DeleteJobModel model)
        {
            _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"DeleteJob\" Parameters: \"{model}\"");
            ValidationResult validationResult = new JobManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            _service.DeleteJob(model.SubmittedJobInfoId, model.SessionCode);
            return Ok("Job deleted.");
        }

        /// <summary>
        ///Get all jobs for user
        /// </summary>
        /// <param name="sessionCode">Session code</param>
        /// <returns></returns>
        [HttpGet("ListJobsForCurrentUser")]
        [RequestSizeLimit(60)]
        [ProducesResponseType(typeof(IEnumerable<SubmittedJobInfoExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult ListJobsForCurrentUser(string sessionCode)
        {
            var model = new ListJobsForCurrentUserModel()
            {
                SessionCode = sessionCode,
            };
            _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"ListJobsForCurrentUser\" Parameters: \"{model}\"");
            ValidationResult validationResult = new JobManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_service.ListJobsForCurrentUser(model.SessionCode));
        }

        /// <summary>
        /// Get current info for job
        /// </summary>
        /// <param name="sessionCode">Session code</param>
        /// <param name="submittedJobInfoId">SubmittedJobInfo ID</param>
        /// <returns></returns>
        [HttpGet("CurrentInfoForJob")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(SubmittedJobInfoExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult CurrentInfoForJob(string sessionCode, long submittedJobInfoId)
        {
            var model = new CurrentInfoForJobModel()
            {
                SessionCode = sessionCode,
                SubmittedJobInfoId = submittedJobInfoId
            };
            _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"CurrentInfoForJob\" Parameters: \"{model}\"");
            ValidationResult validationResult = new JobManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_service.CurrentInfoForJob(model.SubmittedJobInfoId, model.SessionCode));
        }

        /// <summary>
        /// Copy job data to temp folder
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("CopyJobDataToTemp")]
        [RequestSizeLimit(364)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult CopyJobDataToTemp(CopyJobDataToTempModel model)
        {
            _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"CopyJobDataToTemp\" Parameters: \"{model}\"");
            ValidationResult validationResult = new JobManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            _service.CopyJobDataToTemp(model.SubmittedJobInfoId, model.SessionCode, model.Path);
            return Ok("Data were copied to Temp");
        }

        /// <summary>
        /// Copy job data from temp folder
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("CopyJobDataFromTemp")]
        [RequestSizeLimit(154)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult CopyJobDataFromTemp(CopyJobDataFromTempModel model)
        {
            _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"CopyJobDataFromTemp\" Parameters: \"{model}\"");
            ValidationResult validationResult = new JobManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            _service.CopyJobDataFromTemp(model.CreatedJobInfoId, model.SessionCode, model.TempSessionCode);
            return Ok("Data were copied from Temp");
        }

        /// <summary>
        /// Get Allocated Nodes IPs
        /// </summary>
        /// <param name="sessionCode">Session code</param>
        /// <param name="submittedTaskInfoId">SubmittedTaskInfo ID</param>
        /// <returns></returns>
        [HttpGet("AllocatedNodesIPs")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult AllocatedNodesIPs(string sessionCode, long submittedTaskInfoId)
        {
            var model = new AllocatedNodesIPsModel()
            {
                SessionCode = sessionCode,
                SubmittedTaskInfoId = submittedTaskInfoId
            };
            _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"AllocatedNodesIPs\" Parameters: \"{model}\"");
            ValidationResult validationResult = new JobManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_service.AllocatedNodesIPs(model.SubmittedTaskInfoId, model.SessionCode));
        }
        #endregion
    }
}