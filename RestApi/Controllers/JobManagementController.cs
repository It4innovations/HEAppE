using System;
using System.Collections.Generic;
using HEAppE.ServiceTier.JobManagement;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.RestApiModels.JobManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using HEAppE.Utils.Validation;
using HEAppE.RestApi.InputValidator;
using HEAppE.BusinessLogicTier.Logic;
using Microsoft.Extensions.Caching.Memory;

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
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult CreateJob(CreateJobByProjectModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"CreateJob\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.CreateJob(model.JobSpecification, model.SessionCode));
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
        /// Create job specification
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("CreateJobByAccountingString")]
        [RequestSizeLimit(50000)]
        [ProducesResponseType(typeof(SubmittedJobInfoExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult CreateJobByAccountingString(CreateJobByAccountingStringModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"CreateJob\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.CreateJob(model.JobSpecification, model.SessionCode));
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
        /// Submit job
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("SubmitJob")]
        [RequestSizeLimit(94)]
        [ProducesResponseType(typeof(SubmittedJobInfoExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult SubmitJob(SubmitJobModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"SubmitJob\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.SubmitJob(model.CreatedJobInfoId, model.SessionCode));
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
        /// Submit job
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("SubmitJob")]
        [RequestSizeLimit(94)]
        [ProducesResponseType(typeof(SubmittedJobInfoExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [Obsolete]
        public IActionResult Obsolete_SubmitJob(SubmitJobModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"SubmitJob\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.SubmitJob(model.CreatedJobInfoId, model.SessionCode));
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
        /// Cancel job
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("CancelJob")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(SubmittedJobInfoExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult CancelJob(CancelJobModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"CancelJob\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.CancelJob(model.SubmittedJobInfoId, model.SessionCode));
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
        /// Cancel job
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("CancelJob")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(SubmittedJobInfoExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [Obsolete]
        public IActionResult Obsolete_CancelJob(CancelJobModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"CancelJob\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.CancelJob(model.SubmittedJobInfoId, model.SessionCode));
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
        /// Delete job
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpDelete("DeleteJob")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult DeleteJob(DeleteJobModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"DeleteJob\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                _service.DeleteJob(model.SubmittedJobInfoId, model.SessionCode);
                return Ok("Job deleted.");
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
        /// Delete job
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("DeleteJob")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [Obsolete]
        public IActionResult Obsolete_DeleteJob(DeleteJobModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"DeleteJob\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                _service.DeleteJob(model.SubmittedJobInfoId, model.SessionCode);
                return Ok("Job deleted.");
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
        ///Get all jobs for user
        /// </summary>
        /// <param name="sessionCode">Session code</param>
        /// <returns></returns>
        [HttpGet("ListJobsForCurrentUser")]
        [RequestSizeLimit(60)]
        [ProducesResponseType(typeof(IEnumerable<SubmittedJobInfoExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult ListJobsForCurrentUser(string sessionCode)
        {
            try
            {
                var model = new ListJobsForCurrentUserModel()
                {
                    SessionCode = sessionCode,
                };
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"ListJobsForCurrentUser\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.ListJobsForCurrentUser(model.SessionCode));
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
        /// Get all jobs for user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("ListJobsForCurrentUser")]
        [RequestSizeLimit(60)]
        [ProducesResponseType(typeof(IEnumerable<SubmittedJobInfoExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [Obsolete]
        public IActionResult Obsolete_ListJobsForCurrentUser(ListJobsForCurrentUserModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"ListJobsForCurrentUser\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.ListJobsForCurrentUser(model.SessionCode));
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
        /// GetCurrentInfoForJob
        /// </summary>
        /// <param name="sessionCode">Session code</param>
        /// <param name="submittedJobInfoId">SubmittedJobInfo ID</param>
        /// <returns></returns>
        [HttpGet("GetCurrentInfoForJob")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(SubmittedJobInfoExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult GetCurrentInfoForJob(string sessionCode, long submittedJobInfoId)
        {
            try
            {
                var model = new GetCurrentInfoForJobModel()
                {
                    SessionCode = sessionCode,
                    SubmittedJobInfoId = submittedJobInfoId
                };
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"GetCurrentInfoForJob\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetCurrentInfoForJob(model.SubmittedJobInfoId, model.SessionCode));
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
        /// Get current job information
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("GetCurrentInfoForJob")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(SubmittedJobInfoExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [Obsolete]
        public IActionResult Obsolete_GetCurrentInfoForJob(GetCurrentInfoForJobModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"GetCurrentInfoForJob\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetCurrentInfoForJob(model.SubmittedJobInfoId, model.SessionCode));
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
        /// Copy job data to temp folder
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("CopyJobDataToTemp")]
        [RequestSizeLimit(364)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult CopyJobDataToTemp(CopyJobDataToTempModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"CopyJobDataToTemp\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                _service.CopyJobDataToTemp(model.SubmittedJobInfoId, model.SessionCode, model.Path);
                return Ok("Data were copied to Temp");
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
        /// Copy job data from temp folder
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("CopyJobDataFromTemp")]
        [RequestSizeLimit(154)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult CopyJobDataFromTemp(CopyJobDataFromTempModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"CopyJobDataFromTemp\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                _service.CopyJobDataFromTemp(model.CreatedJobInfoId, model.SessionCode, model.TempSessionCode);
                return Ok("Data were copied to Temp");
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
        /// 
        /// </summary>
        /// <param name="sessionCode">Session code</param>
        /// <param name="submittedTaskInfoId">SubmittedTaskInfo ID</param>
        /// <returns></returns>
        [HttpGet("GetAllocatedNodesIPs")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult GetAllocatedNodesIPs(string sessionCode, long submittedTaskInfoId)
        {
            try
            {
                var model = new GetAllocatedNodesIPsModel()
                {
                    SessionCode = sessionCode,
                    SubmittedTaskInfoId = submittedTaskInfoId
                };
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"GetAllocatedNodesIPs\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetAllocatedNodesIPs(model.SubmittedTaskInfoId, model.SessionCode));
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
        /// Get allocated node IP addresses
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("GetAllocatedNodesIPs")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [Obsolete]
        public IActionResult Obsolete_GetAllocatedNodesIPs(GetAllocatedNodesIPsModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"JobManagement\" Method: \"GetAllocatedNodesIPs\" Parameters: \"{model}\"");
                ValidationResult validationResult = new JobManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetAllocatedNodesIPs(model.SubmittedTaskInfoId, model.SessionCode));
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
