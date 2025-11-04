using System.Collections.Generic;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.FileTransfer;
using HEAppE.ServiceTier.FileTransfer;
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
    public FileTransferController(ILogger<FileTransferController> logger, IMemoryCache memoryCache, ISshCertificateAuthorityService sshCertificateAuthorityService) : base(logger,
        memoryCache)
    {
        _service = new FileTransferService(sshCertificateAuthorityService);
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
    public IActionResult RequestFileTransfer(GetFileTransferMethodModel model)
    {
        _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"RequestFileTransfer\" Parameters: \"{model}\"");
        var validationResult = new FileTransferValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(_service.RequestFileTransfer(model.SubmittedJobInfoId, model.SessionCode));
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

    #endregion
}