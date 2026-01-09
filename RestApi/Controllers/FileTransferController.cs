using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using HEAppE.BusinessLogicTier;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.General.Models;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.FileTransfer;
using HEAppE.ServiceTier.FileTransfer;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using log4net.Repository.Hierarchy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.RestApi.Controllers;

[ApiController]
[Route("heappe/[controller]")]
[Produces("application/json")]
public class FileTransferController : BaseController<FileTransferController>
{
    #region Instances

    private readonly IFileTransferService _service;

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="memoryCache">Memory cache provider</param>
    public FileTransferController(ILogger<FileTransferController> logger, IMemoryCache memoryCache, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys) : base(logger,
        memoryCache)
    {
        _service = new FileTransferService(sshCertificateAuthorityService, httpContextKeys);
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Create file transfer tunnel
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("RequestFileTransfer")]
    [RequestSizeLimit(98)]
    [ProducesResponseType(typeof(FileTransferMethodExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RequestFileTransfer(GetFileTransferMethodModel model)
    {
        _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"RequestFileTransfer\" Parameters: \"{model}\"");
        var validationResult = new FileTransferValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.RequestFileTransfer(model.SubmittedJobInfoId, model.SessionCode));
    }

    /// <summary>
    ///     Close file transfer tunnel
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("CloseFileTransfer")]
    [RequestSizeLimit(4700)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CloseFileTransfer(EndFileTransferModel model)
    {
        _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"CloseFileTransfer\" Parameters: \"{model}\"");

        var validationResult = new FileTransferValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _service.CloseFileTransfer(model.SubmittedJobInfoId, model.PublicKey, model.SessionCode);
        return Ok("File transfer closed");
    }

    /// <summary>
    ///     Download part of job files from Cluster
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("DownloadPartsOfJobFilesFromCluster")]
    [RequestSizeLimit(480)]
    [ProducesResponseType(typeof(IEnumerable<JobFileContentExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult DownloadPartsOfJobFilesFromCluster(DownloadPartsOfJobFilesFromClusterModel model)
    {
        _logger.LogDebug(
            $"Endpoint: \"FileTransfer\" Method: \"DownloadPartsOfJobFilesFromCluster\" Parameters: \"{model}\"");
        var validationResult = new FileTransferValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_service.DownloadPartsOfJobFilesFromCluster(model.SubmittedJobInfoId, model.TaskFileOffsets,
            model.SessionCode));
    }

    /// <summary>
    ///     Get all changes files during job execution
    /// </summary>
    /// <param name="sessionCode">Session code</param>
    /// <param name="submittedJobInfoId">SubmittedJobInfo ID</param>
    /// <returns></returns>
    [HttpGet("ListChangedFilesForJob")]
    [RequestSizeLimit(98)]
    [ProducesResponseType(typeof(IEnumerable<FileInformationExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ListChangedFilesForJob(string sessionCode, long submittedJobInfoId)
    {
        var model = new ListChangedFilesForJobModel
        {
            SessionCode = sessionCode,
            SubmittedJobInfoId = submittedJobInfoId
        };
        _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"ListChangedFilesForJob\" Parameters: \"{model}\"");
        var validationResult = new FileTransferValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_service.ListChangedFilesForJob(model.SubmittedJobInfoId, model.SessionCode));
    }

    /// <summary>
    ///     Download specific file from Cluster
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("DownloadFileFromCluster")]
    [RequestSizeLimit(700)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult DownloadFileFromCluster(DownloadFileFromClusterModel model)
    {
        _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"DownloadFileFromCluster\" Parameters: \"{model}\"");
        var validationResult = new FileTransferValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_service.DownloadFileFromCluster(model.SubmittedJobInfoId, model.RelativeFilePath,
            model.SessionCode));
    }

    static List<FileUploadResultExt> doExtractFilesUploadResult(IFormFileCollection files, List<Task<dynamic>> tasks)
    {
        var result = new List<FileUploadResultExt>();
        for (var i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            var file = files[i];
            var item = new FileUploadResultExt() { FileName = file.FileName, Succeeded = false, Path = null };
            result.Add(item);

            Dictionary<string, dynamic> taskResult = task.Result;
            if (taskResult == null)
                continue;
            item.Succeeded = taskResult["Succeeded"];
            item.Path = taskResult["Path"];
        }
        return result;
    }

    /// <summary>
    ///     Upload job to file to job execution dir
    /// </summary>
    /// <param name="sessionCode">sessionCode</param>
    /// <param name="createdJobInfoId">createdJobInfoId</param>
    /// <param name="files">files</param>
    /// <param name="sshCertificateAuthorityService">sshCertificateAuthorityService</param>
    /// <param name="httpContextKeys">httpContextKeys</param>
    /// <returns></returns>
    [HttpPost("UploadFilesToJobExecutionDir")]
    [RequestSizeLimit(2_200_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 2_200_000_000)]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ICollection<FileUploadResultExt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult UploadFilesToJobExecutionDir(
        [FromQuery(Name = "SessionCode")] string sessionCode,
        [FromQuery(Name = "JobId")] long jobId,
        [FromQuery(Name = "TaskId")] long? taskId,
        [FromForm] IFormFileCollection files,
        [FromServices] ISshCertificateAuthorityService sshCertificateAuthorityService,
        [FromServices] IHttpContextKeys httpContextKeys
    )
    {
        var model = new UploadFileToClusterModel() { SessionCode = sessionCode };
        var validator = new UploadFileToClusterModelValidator();
        validator.ValidateAndThrow(model);
        _logger.LogDebug("""Endpoint: "FileTransfer" Method: "UploadFileToClusterModel" Parameters: "{@model}" """, model);

        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var job = unitOfWork.JobSpecificationRepository.GetById(jobId) ??
                      throw new Exception("NotExistingJob");
            //check if task belongs to job
            if (taskId.HasValue)
            {
                if(!job.Tasks.Any(t=> t.Id == taskId.Value))
                    throw new Exception("TaskDoesNotBelongToJob");
            }
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, sshCertificateAuthorityService, httpContextKeys,
                            AdaptorUserRoleType.Submitter, job.ProjectId);
            if (job.Submitter.Id != loggedUser.Id)
                throw new Exception("LoggedUserIsNotSubmitterOfJob");
        }

        var tasks = new List<Task<dynamic>>();
        foreach (var file in files)
        {
            tasks.Add(new FileTransferService(sshCertificateAuthorityService, httpContextKeys).UploadFileToJobExecutionDir(file.OpenReadStream(), file.FileName, jobId, taskId, sessionCode));
        }
        Task.WaitAll(tasks);

        List<FileUploadResultExt> result = doExtractFilesUploadResult(files, tasks);
        return Ok(result);
    }

    #endregion
}