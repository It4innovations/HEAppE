using HEAppE.Exceptions.External;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.FileTransfer;
using HEAppE.ServiceTier.FileTransfer;
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
    public class FileTransferController : BaseController<FileTransferController>
    {
        #region Instances
        private IFileTransferService _service = new FileTransferService();
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="memoryCache">Memory cache provider</param>
        public FileTransferController(ILogger<FileTransferController> logger, IMemoryCache memoryCache) : base(logger, memoryCache)
        {

        }
        #endregion
        #region Methods
        /// <summary>
        /// Create file transfer tunnel
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("RequestFileTransfer")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(FileTransferMethodExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult RequestFileTransfer(GetFileTransferMethodModel model)
        {
            _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"RequestFileTransfer\" Parameters: \"{model}\"");
            ValidationResult validationResult = new FileTransferValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_service.RequestFileTransfer(model.SubmittedJobInfoId, model.SessionCode));
        }

        /// <summary>
        /// Close file transfer tunnel
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("CloseFileTransfer")]
        [RequestSizeLimit(4700)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult CloseFileTransfer(EndFileTransferModel model)
        {
            _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"CloseFileTransfer\" Parameters: \"{model}\"");

            ValidationResult validationResult = new FileTransferValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            _service.CloseFileTransfer(model.SubmittedJobInfoId, model.PublicKey, model.SessionCode);
            return Ok("CloseFileTransfer");
        }

        /// <summary>
        /// Download part of job files from Cluster
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("DownloadPartsOfJobFilesFromCluster")]
        [RequestSizeLimit(480)]
        [ProducesResponseType(typeof(IEnumerable<JobFileContentExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult DownloadPartsOfJobFilesFromCluster(DownloadPartsOfJobFilesFromClusterModel model)
        {
            _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"DownloadPartsOfJobFilesFromCluster\" Parameters: \"{model}\"");
            ValidationResult validationResult = new FileTransferValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_service.DownloadPartsOfJobFilesFromCluster(model.SubmittedJobInfoId, model.TaskFileOffsets, model.SessionCode));
        }

        /// <summary>
        /// Get all changes files during job execution
        /// </summary>
        /// <param name="sessionCode">Session code</param>
        /// <param name="submittedJobInfoId">SubmittedJobInfo ID</param>
        /// <returns></returns>
        [HttpGet("ListChangedFilesForJob")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(IEnumerable<FileInformationExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult ListChangedFilesForJob(string sessionCode, long submittedJobInfoId)
        {
            var model = new ListChangedFilesForJobModel()
            {
                SessionCode = sessionCode,
                SubmittedJobInfoId = submittedJobInfoId
            };
            _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"ListChangedFilesForJob\" Parameters: \"{model}\"");
            ValidationResult validationResult = new FileTransferValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_service.ListChangedFilesForJob(model.SubmittedJobInfoId, model.SessionCode));
        }

        /// <summary>
        /// Download specific file from Cluster
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("DownloadFileFromCluster")]
        [RequestSizeLimit(378)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult DownloadFileFromCluster(DownloadFileFromClusterModel model)
        {
            _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"DownloadFileFromCluster\" Parameters: \"{model}\"");
            ValidationResult validationResult = new FileTransferValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_service.DownloadFileFromCluster(model.SubmittedJobInfoId, model.RelativeFilePath, model.SessionCode));
        }
        #endregion
    }
}