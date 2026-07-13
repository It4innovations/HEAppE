using System;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.JobManagement;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using HEAppE.ServiceTier.JobManagement;
using HEAppE.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SshCaAPI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.RestApi.Logging;

using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;

namespace HEAppE.RestApi.Controllers;

[ApiController]
[Route("heappe/[controller]")]
[Produces("application/json")]
public class JobManagementController : BaseController<JobManagementController>
{
    #region Instances

    private readonly IJobManagementService _service;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IExpirioService _expirioService;

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="memoryCache">Memory cache provider</param>
    /// <param name="userOrgService"></param>
    /// <param name="sshCertificateAuthorityService">SSH Certificate Authority service</param>
    /// <param name="httpContextKeys"></param>
    public JobManagementController(ILogger<JobManagementController> logger, IMemoryCache memoryCache, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys, IExpirioService expirioService) : base(logger,
        memoryCache)
    {
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _expirioService = expirioService;
        _service = new JobManagementService(userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, memoryCache, _logger);
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Create job specification
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("CreateJob")]
    [RequestSizeLimit(250000)]
    [LogBehavior(LoggingBehavior.HeadersOnly)]
    [ProducesResponseType(typeof(SubmittedJobInfoExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateJob(CreateJobByProjectModel model)
    {
        var validationResult = new JobManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.CreateJob(model.JobSpecification, model.SessionCode));
    }

    /// <summary>
    ///     Submit job
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
    public async Task<IActionResult> SubmitJob(SubmitJobModel model)
    {
        var validationResult = new JobManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.SubmitJobAsync(model.CreatedJobInfoId, model.SessionCode));
    }

    /// <summary>
    ///     Cancel job
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
    public async Task<IActionResult> CancelJob(CancelJobModel model)
    {
        var validationResult = new JobManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.CancelJob(model.SubmittedJobInfoId, model.SessionCode));
    }

    /// <summary>
    ///     Delete job
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("DeleteJob")]
    [RequestSizeLimit(120)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteJob(DeleteJobModel model)
    {
        var validationResult = new JobManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        var isDeleted = await _service.DeleteJob(model.SubmittedJobInfoId, model.ArchiveLogs, model.SessionCode);
        if (isDeleted)
            return Ok("Job was deleted");
        return BadRequest("Job was not deleted");
    }

    /// <summary>
    ///     Get all jobs for user
    /// </summary>
    /// <param name="sessionCode">Session code</param>
    /// <param name="jobStates">
    ///     Job states separated by coma; eg.: "1,2,8,16,32"
    /// </param>
    /// <param name="limit">Max number of jobs to return</param>
    /// <param name="offset">Number of jobs to skip</param>
    /// <param name="userId">Filter by user ID</param>
    /// <param name="clusterId">Filter by cluster ID</param>
    /// <param name="subProjectId">Filter by subproject ID</param>
    /// <param name="projectId">Filter by project ID</param>
    /// <returns></returns>
    [HttpGet("ListJobsForCurrentUser")]
    [RequestSizeLimit(60)]
    [ProducesResponseType(typeof(IEnumerable<SubmittedJobInfoExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListJobsForCurrentUser(string sessionCode, string jobStates = null, int? limit = null, int? offset = null, long? userId = null, long? clusterId = null, long? subProjectId = null, long? projectId = null)
    {
        var model = new ListJobsForCurrentUserModel
        {
            SessionCode = sessionCode
        };
        var validationResult = new JobManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.ListJobsForCurrentUser(model.SessionCode, jobStates, limit, offset, userId, clusterId, subProjectId, projectId));
    }

    /// <summary>
    ///     Get current info for job
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
    public async Task<IActionResult> CurrentInfoForJob(string sessionCode, long submittedJobInfoId)
    {
        var model = new CurrentInfoForJobModel
        {
            SessionCode = sessionCode,
            SubmittedJobInfoId = submittedJobInfoId
        };
        var validationResult = new JobManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.CurrentInfoForJob(model.SubmittedJobInfoId, model.SessionCode));
    }

    /// <summary>
    ///     Copy job data to temp folder
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
    public async Task<IActionResult> CopyJobDataToTemp(CopyJobDataToTempModel model)
    {
        var validationResult = new JobManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        await _service.CopyJobDataToTempAsync(model.CreatedJobInfoId, model.SessionCode, model.Path);
        return Ok("Data were copied to Temp");
    }

    /// <summary>
    ///     Copy job data from temp folder
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
    public async Task<IActionResult> CopyJobDataFromTemp(CopyJobDataFromTempModel model)
    {
        var validationResult = new JobManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        await _service.CopyJobDataFromTempAsync(model.CreatedJobInfoId, model.SessionCode, model.TempSessionCode);
        return Ok("Data were copied from Temp");
    }

    /// <summary>
    ///     Get Allocated Nodes IPs
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
    public async Task<IActionResult> AllocatedNodesIPs(string sessionCode, long submittedTaskInfoId)
    {
        var model = new AllocatedNodesIPsModel
        {
            SessionCode = sessionCode,
            SubmittedTaskInfoId = submittedTaskInfoId
        };
        var validationResult = new JobManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.AllocatedNodesIPsAsync(model.SubmittedTaskInfoId, model.SessionCode));
    }
    
    /// <summary>
    ///     Dry run job
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("DryRunJob")]
    [RequestSizeLimit(250000)]
    [LogBehavior(LoggingBehavior.HeadersOnly)]
    [ProducesResponseType(typeof(DryRunJobInfoExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DryRunJob(DryRunJobModel model)
    {
        var validationResult = new JobManagementValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.DryRunJob(model.ProjectId, model.ClusterNodeTypeId, model.Nodes, model.TasksPerNode, model.WallTimeInMinutes, model.SessionCode));
    }

    /// <summary>
    ///     Update task status callback
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("TaskCallback")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TaskCallback([FromBody] TaskCallbackModel model)
    {
        if (model == null || string.IsNullOrEmpty(model.Token))
        {
            return BadRequest("Invalid callback payload. Token is required.");
        }

        string scheduledJobId = model.ScheduledJobId;
        if (string.IsNullOrEmpty(scheduledJobId) && !string.IsNullOrEmpty(model.SessionId))
        {
            scheduledJobId = $"session:{model.SessionId}";
        }

        if (string.IsNullOrEmpty(scheduledJobId))
        {
            return BadRequest("Invalid callback payload. task_id or session_id is required.");
        }

        await _service.ProcessTaskCallbackAsync(scheduledJobId, model.Token, model.RawResponse, model.QSchedulerState);
        return Ok("Task status updated successfully.");
    }

    /// <summary>
    /// Open QScheduler session explicitly
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("OpenQSchedulerSession")]
    [ProducesResponseType(typeof(long), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OpenQSchedulerSession([FromBody] OpenQSchedulerSessionModel model)
    {
        if (model == null)
        {
            return BadRequest("Model is empty");
        }

        var sessionId = await _service.OpenQSchedulerSessionAsync(model.ClusterId, model.ProjectId, model.MachineId, model.WalltimeLimit, model.SessionCode);
        return Ok(sessionId);
    }

    /// <summary>
    /// Close QScheduler session explicitly
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpDelete("CloseQSchedulerSession")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CloseQSchedulerSession([FromBody] CloseQSchedulerSessionModel model)
    {
        if (model == null)
        {
            return BadRequest("Model is empty");
        }

        await _service.CloseQSchedulerSessionAsync(model.ClusterId, model.ProjectId, model.SessionId, model.SessionCode);
        return Ok("Session closed successfully.");
    }

    /// <summary>
    ///     Get QScheduler session info and state
    /// </summary>
    /// <param name="sessionCode">HEAppE session code</param>
    /// <param name="sessionId">QScheduler session ID (returned by OpenQSchedulerSession)</param>
    /// <returns>Session info including current state, owner, timestamps</returns>
    [HttpGet("QSchedulerSessionInfo")]
    [ProducesResponseType(typeof(QSchedulerSessionInfoExt), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> QSchedulerSessionInfo(string sessionCode, long sessionId)
    {
        if (sessionId <= 0)
            return BadRequest("sessionId is required.");

        var info = await _service.GetQSchedulerSessionInfoAsync(sessionId, sessionCode);
        return Ok(info);
    }

    /// <summary>
    ///     Create a QScheduler job with simplified specification and optional multipart payload upload per task, and submit it immediately.
    /// </summary>
    /// <param name="model">JSON string of CreateAndSubmitQSchedulerJobModel</param>
    /// <param name="userOrgService">User org service</param>
    /// <returns>Submitted job info</returns>
    [HttpPost("CreateAndSubmitQSchedulerJob")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2_200_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 2_200_000_000)]
    [ProducesResponseType(typeof(SubmittedJobInfoExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAndSubmitQSchedulerJob(
        [FromForm] string model,
        [FromServices] IUserOrgService userOrgService)
    {
        if (string.IsNullOrEmpty(model))
        {
            return BadRequest("Model string is empty.");
        }

        CreateAndSubmitQSchedulerJobModel parsedModel;
        try
        {
            parsedModel = System.Text.Json.JsonSerializer.Deserialize<CreateAndSubmitQSchedulerJobModel>(model, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (System.Exception ex)
        {
            return BadRequest($"Invalid model JSON: {ex.Message}");
        }

        if (parsedModel == null)
        {
            return BadRequest("Failed to parse model.");
        }

        var validationResult = new JobManagementValidator(parsedModel).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        // Additional validation for multipart files existence
        foreach (var task in parsedModel.JobSpecification.Tasks)
        {
            if (!string.IsNullOrEmpty(task.PayloadPartName))
            {
                var file = Request.Form.Files[task.PayloadPartName];
                if (file == null)
                {
                    return BadRequest($"Multipart file for PayloadPartName '{task.PayloadPartName}' was not provided in the request.");
                }
            }
        }

        // 1. Determine connection protocol of the cluster
        HEAppE.DomainObjects.ClusterInformation.ClusterConnectionProtocol connectionProtocol;
        using (var unitOfWork = HEAppE.DataAccessTier.Factory.UnitOfWork.UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var cluster = unitOfWork.ClusterRepository.GetById(parsedModel.JobSpecification.ClusterId);
            if (cluster == null) return NotFound("Cluster not found.");
            connectionProtocol = cluster.ConnectionProtocol;
        }

        var payloads = new Dictionary<string, System.IO.Stream>();
        HEAppE.ExtModels.JobManagement.Models.SubmittedJobInfoExt createdJob = null;
        try
        {
            if (connectionProtocol == HEAppE.DomainObjects.ClusterInformation.ClusterConnectionProtocol.Http ||
                connectionProtocol == HEAppE.DomainObjects.ClusterInformation.ClusterConnectionProtocol.Https)
            {
                // 2a. HTTP Mode: Read uploaded files directly as request streams (no memory buffers/RAM copy)
                for (int i = 0; i < parsedModel.JobSpecification.Tasks.Length; i++)
                {
                    var qTask = parsedModel.JobSpecification.Tasks[i];
                    if (!string.IsNullOrEmpty(qTask.PayloadPartName))
                    {
                        var file = Request.Form.Files[qTask.PayloadPartName];
                        payloads[qTask.Name] = file.OpenReadStream();
                    }
                }
                HEAppE.Utils.QSchedulerPayloadContext.Payloads = payloads;
            }

            // 3. Create the job database records (metadata only, no payload saved to DB)
            createdJob = await _service.CreateAndSubmitQSchedulerJob(parsedModel.JobSpecification, parsedModel.SessionCode);

            if (connectionProtocol == HEAppE.DomainObjects.ClusterInformation.ClusterConnectionProtocol.Ssh)
            {
                // 2b. SSH Mode: Upload files directly via SFTP (streams directly to cluster filesystem)
                var fileTransferService = new HEAppE.ServiceTier.FileTransfer.FileTransferService(
                    userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);

                for (int i = 0; i < parsedModel.JobSpecification.Tasks.Length; i++)
                {
                    var qTask = parsedModel.JobSpecification.Tasks[i];
                    if (!string.IsNullOrEmpty(qTask.PayloadPartName))
                    {
                        var file = Request.Form.Files[qTask.PayloadPartName];
                        var createdTask = createdJob.Tasks.FirstOrDefault(t => t.Name == qTask.Name);
                        if (createdTask == null || !createdTask.Id.HasValue)
                        {
                            throw new System.Exception($"Failed to find matching created task for upload: '{qTask.Name}'");
                        }

                        using (var stream = file.OpenReadStream())
                        {
                            var uploadResult = await fileTransferService.UploadFileToJobExecutionDir(
                                stream, "payload.json", createdJob.Id.Value, createdTask.Id.Value, parsedModel.SessionCode);
                            
                            if (uploadResult == null || !uploadResult.ContainsKey("Succeeded") || uploadResult["Succeeded"] == false)
                            {
                                throw new System.Exception($"Failed to upload circuit payload for task '{qTask.Name}'.");
                            }
                        }
                    }
                }
            }

            // 4. Immediately submit the job to QScheduler (adapter will read from context or cluster path)
            var submittedJob = await _service.SubmitJobAsync(createdJob.Id.Value, parsedModel.SessionCode);
            return Ok(submittedJob);
        }
        catch (System.Exception submitEx)
        {
            _logger.LogError(submitEx, $"Automatic submission of QScheduler job failed.");
            if (createdJob != null && createdJob.Id.HasValue)
            {
                // Try to clean up created job if submission failed
                try
                {
                    await _service.DeleteJob(createdJob.Id.Value, false, parsedModel.SessionCode);
                }
                catch (System.Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, $"Cleanup of job {createdJob.Id.Value} failed after submission failure.");
                }
            }
            throw;
        }
        finally
        {
            // 5. Clean up open request streams and reset context
            if (HEAppE.Utils.QSchedulerPayloadContext.Payloads != null)
            {
                foreach (var stream in HEAppE.Utils.QSchedulerPayloadContext.Payloads.Values)
                {
                    stream?.Dispose();
                }
                HEAppE.Utils.QSchedulerPayloadContext.Payloads = null;
            }
        }
    }

    /// <summary>
    ///     List QScheduler sessions with optional state, cluster, and project filters.
    /// </summary>
    /// <param name="sessionCode">HEAppE session code</param>
    /// <param name="state">Optional state filter ("Open" or "Closed")</param>
    /// <param name="clusterId">Optional cluster ID filter</param>
    /// <param name="projectId">Optional project ID filter</param>
    /// <returns>List of session specifications including their state history (created/closed timestamps)</returns>
    [HttpGet("ListQSchedulerSessions")]
    [ProducesResponseType(typeof(IEnumerable<QSchedulerSessionInfoExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListQSchedulerSessions(string sessionCode, string state = null, long? clusterId = null, long? projectId = null)
    {

        var sessions = await _service.ListQSchedulerSessionsAsync(sessionCode, state, clusterId, projectId);
        return Ok(sessions);
    }

    #endregion
}