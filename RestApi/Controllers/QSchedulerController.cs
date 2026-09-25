using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.ClusterInformation;
using HEAppE.RestApiModels.JobManagement;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using HEAppE.ServiceTier.ClusterInformation;
using HEAppE.ServiceTier.JobManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.RestApi.Controllers;

/// <summary>
///     QScheduler &amp; Quantum Computing Endpoint
/// </summary>
[ApiController]
[Route("heappe/[controller]")]
[Produces("application/json")]
public class QSchedulerController : BaseController<QSchedulerController>
{
    private readonly IJobManagementService _jobService;
    private readonly IClusterInformationService _clusterService;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IExpirioService _expirioService;

    public QSchedulerController(
        ILogger<QSchedulerController> logger,
        IMemoryCache memoryCache,
        IUserOrgService userOrgService,
        ISshCertificateAuthorityService sshCertificateAuthorityService,
        IHttpContextKeys httpContextKeys,
        IExpirioService expirioService) : base(logger, memoryCache)
    {
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _expirioService = expirioService;
        _jobService = new JobManagementService(userOrgService, sshCertificateAuthorityService, httpContextKeys, expirioService, memoryCache, logger);
        _clusterService = new ClusterInformationService(memoryCache, userOrgService, sshCertificateAuthorityService, httpContextKeys, expirioService, logger);
    }

    #region Sessions

    /// <summary>
    /// Open QScheduler session explicitly
    /// </summary>
    /// <param name="model">Session specification</param>
    /// <returns>Session ID</returns>
    [HttpPost("OpenSession")]
    [ProducesResponseType(typeof(long), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OpenSession([FromBody] OpenQSchedulerSessionModel model)
    {
        if (model == null)
        {
            return BadRequest("Model is empty");
        }

        var sessionId = await _jobService.OpenQSchedulerSessionAsync(model.ClusterId, model.ProjectId, model.MachineId, model.WalltimeLimit, model.SessionCode);
        return Ok(sessionId);
    }

    /// <summary>
    /// Close QScheduler session explicitly
    /// </summary>
    /// <param name="model">Session identification</param>
    /// <returns>Status message</returns>
    [HttpDelete("CloseSession")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CloseSession([FromBody] CloseQSchedulerSessionModel model)
    {
        if (model == null)
        {
            return BadRequest("Model is empty");
        }

        await _jobService.CloseQSchedulerSessionAsync(model.ClusterId, model.ProjectId, model.SessionId, model.SessionCode);
        return Ok("Session closed successfully.");
    }

    /// <summary>
    ///     Get QScheduler session info and state
    /// </summary>
    /// <param name="sessionCode">HEAppE session code</param>
    /// <param name="sessionId">QScheduler session ID (returned by OpenSession)</param>
    /// <returns>Session info including current state, owner, timestamps</returns>
    [HttpGet("GetSessionInfo")]
    [ProducesResponseType(typeof(QSchedulerSessionInfoExt), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSessionInfo(string sessionCode, long sessionId)
    {
        if (sessionId <= 0)
            return BadRequest("sessionId is required.");

        var info = await _jobService.GetQSchedulerSessionInfoAsync(sessionId, sessionCode);
        return Ok(info);
    }

    /// <summary>
    ///     List QScheduler sessions with optional state, cluster, and project filters.
    /// </summary>
    /// <param name="sessionCode">HEAppE session code</param>
    /// <param name="state">Optional state filter ("Open" or "Closed")</param>
    /// <param name="clusterId">Optional cluster ID filter</param>
    /// <param name="projectId">Optional project ID filter</param>
    /// <returns>List of session specifications including their state history (created/closed timestamps)</returns>
    [HttpGet("ListSessions")]
    [ProducesResponseType(typeof(IEnumerable<QSchedulerSessionInfoExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListSessions(string sessionCode, string state = null, long? clusterId = null, long? projectId = null)
    {
        var sessions = await _jobService.ListQSchedulerSessionsAsync(sessionCode, state, clusterId, projectId);
        return Ok(sessions);
    }

    #endregion

    #region Jobs &amp; Tasks

    /// <summary>
    ///     Create a QScheduler job with simplified specification and optional multipart payload upload per task, and submit it immediately.
    /// </summary>
    /// <remarks>
    /// Sample JSON structure to enter into the 'model' form field:
    /// 
    ///     {
    ///       "SessionCode": "your-heappe-session-code",
    ///       "JobSpecification": {
    ///         "Name": "QuantumJob",
    ///         "ClusterId": 1,
    ///         "ProjectId": 1,
    ///         "Tasks": [
    ///           {
    ///             "Name": "GroverTask",
    ///             "MachineId": "iqm_simulator",
    ///             "WalltimeLimitSecs": 3600,
    ///             "PayloadPartName": "circuit1",
    ///             "UseSessions": false
    ///           }
    ///         ]
    ///       }
    ///     }
    /// 
    /// Note: Attach your circuit payload file in the multipart/form-data request using the key name specified in 'PayloadPartName' (e.g. 'circuit1').
    /// </remarks>
    /// <param name="model">JSON string of CreateAndSubmitQSchedulerJobModel (see remarks for example JSON format)</param>
    /// <param name="userOrgService">User org service</param>
    /// <returns>Submitted job info</returns>
    [HttpPost("CreateAndSubmitJob")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2_200_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 2_200_000_000)]
    [ProducesResponseType(typeof(SubmittedJobInfoExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAndSubmitJob(
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
            createdJob = await _jobService.CreateAndSubmitQSchedulerJob(parsedModel.JobSpecification, parsedModel.SessionCode);

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
            var submittedJob = await _jobService.SubmitJobAsync(createdJob.Id.Value, parsedModel.SessionCode);
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
                    await _jobService.DeleteJob(createdJob.Id.Value, false, parsedModel.SessionCode);
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
    ///     Get QScheduler task result (IQM metadata, timeline, etc.)
    /// </summary>
    /// <param name="sessionCode">HEAppE session code</param>
    /// <param name="submittedTaskId">HEAppE Task ID</param>
    [HttpGet("GetTaskResult")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTaskResult(string sessionCode, long submittedTaskId)
    {
        var stream = await _jobService.GetQuantumTaskResultAsync(submittedTaskId, sessionCode);
        return File(stream, "application/json");
    }

    /// <summary>
    ///     Get QScheduler task artifact by name.
    /// </summary>
    /// <param name="sessionCode">HEAppE session code</param>
    /// <param name="submittedTaskId">HEAppE Task ID</param>
    /// <param name="artifactName">Artifact name (e.g. measurements, measurements_counts, sweep_results)</param>
    [HttpGet("GetTaskArtifact")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTaskArtifact(string sessionCode, long submittedTaskId, string artifactName)
    {
        if (string.IsNullOrEmpty(artifactName))
        {
            throw new Exceptions.External.InputValidationException("ArtifactNameMustBeSpecified", "artifactName");
        }
        var stream = await _jobService.GetQuantumTaskArtifactAsync(submittedTaskId, artifactName, sessionCode);
        return File(stream, "application/octet-stream");
    }

    #endregion

    #region Hardware &amp; Calibration

    /// <summary>
    ///     Get QScheduler machine architecture
    /// </summary>
    /// <returns>JSON string with machine architecture topology</returns>
    [HttpGet("MachineArchitecture")]
    [HttpGet("/heappe/ClusterInformation/MachineArchitecture")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MachineArchitecture([FromQuery] GetMachineArchitectureModel model)
    {
        _logger.LogInformation($"MachineArchitecture API request received. ClusterNodeTypeId: {model?.ClusterNodeTypeId}, ProjectId: {model?.ProjectId}");
        var validationResult = new ClusterInformationValidator(model).Validate();
        if (!validationResult.IsValid)
        {
            _logger.LogWarning($"MachineArchitecture validation failed: {validationResult.Message}");
            throw new InputValidationException(validationResult.Message);
        }

        var result = await _clusterService.GetMachineArchitecture(model.ClusterNodeTypeId, model.ProjectId, model.SessionCode);
        _logger.LogInformation($"MachineArchitecture API request completed. ClusterNodeTypeId: {model.ClusterNodeTypeId}");
        return Content(result, "application/json");
    }

    /// <summary>
    ///     Get QScheduler machine calibration
    /// </summary>
    /// <returns>JSON string with machine calibration parameters</returns>
    [HttpGet("MachineCalibration")]
    [HttpGet("/heappe/ClusterInformation/MachineCalibration")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MachineCalibration([FromQuery] GetMachineCalibrationModel model)
    {
        _logger.LogInformation($"MachineCalibration API request received. ClusterNodeTypeId: {model?.ClusterNodeTypeId}, CalibrationId: '{model?.CalibrationId}', Endpoint: '{model?.Endpoint}', ProjectId: {model?.ProjectId}");
        var validationResult = new ClusterInformationValidator(model).Validate();
        if (!validationResult.IsValid)
        {
            _logger.LogWarning($"MachineCalibration validation failed: {validationResult.Message}");
            throw new InputValidationException(validationResult.Message);
        }

        var result = await _clusterService.GetMachineCalibration(model.ClusterNodeTypeId, model.CalibrationId, model.Endpoint, model.ProjectId, model.SessionCode);
        _logger.LogInformation($"MachineCalibration API request completed. ClusterNodeTypeId: {model.ClusterNodeTypeId}");
        return Content(result, "application/json");
    }

    /// <summary>
    ///     Get QScheduler machine info
    /// </summary>
    /// <returns>JSON string with machine info</returns>
    [HttpGet("MachineInfo")]
    [HttpGet("/heappe/ClusterInformation/MachineInfo")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MachineInfo([FromQuery] GetMachineInfoModel model)
    {
        _logger.LogInformation($"MachineInfo API request received. ClusterNodeTypeId: {model?.ClusterNodeTypeId}, ProjectId: {model?.ProjectId}");
        var validationResult = new ClusterInformationValidator(model).Validate();
        if (!validationResult.IsValid)
        {
            _logger.LogWarning($"MachineInfo validation failed: {validationResult.Message}");
            throw new InputValidationException(validationResult.Message);
        }

        var result = await _clusterService.GetMachineInfo(model.ClusterNodeTypeId, model.ProjectId, model.SessionCode);
        _logger.LogInformation($"MachineInfo API request completed. ClusterNodeTypeId: {model.ClusterNodeTypeId}");
        return Content(result, "application/json");
    }

    #endregion
}

