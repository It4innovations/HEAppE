using HEAppE.BusinessLogicTier.Logic;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.FileTransfer;
using HEAppE.ServiceTier.FileTransfer;
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
        [HttpPost("GetFileTransferMethod")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(FileTransferMethodExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetFileTransferMethod(GetFileTransferMethodModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"GetFileTransferMethod\" Parameters: \"{model}\"");
                ValidationResult validationResult = new FileTransferValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.GetFileTransferMethod(model.SubmittedJobInfoId, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// End file transfer tunnel
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("EndFileTransfer")]
        [RequestSizeLimit(4600)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult EndFileTransfer(EndFileTransferModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"EndFileTransfer\" Parameters: \"{model}\"");

                ValidationResult validationResult = new FileTransferValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                _service.EndFileTransfer(model.SubmittedJobInfoId, model.UsedTransferMethod, model.SessionCode);
                return Ok("EndFileTransfer");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
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
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult DownloadPartsOfJobFilesFromCluster(DownloadPartsOfJobFilesFromClusterModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"DownloadPartsOfJobFilesFromCluster\" Parameters: \"{model}\"");
                ValidationResult validationResult = new FileTransferValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.DownloadPartsOfJobFilesFromCluster(model.SubmittedJobInfoId, model.TaskFileOffsets, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Get all changes files during job execution
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("ListChangedFilesForJob")]
        [RequestSizeLimit(98)]
        [ProducesResponseType(typeof(IEnumerable<FileInformationExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ListChangedFilesForJob(ListChangedFilesForJobModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"ListChangedFilesForJob\" Parameters: \"{model}\"");
                ValidationResult validationResult = new FileTransferValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.ListChangedFilesForJob(model.SubmittedJobInfoId, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Download specific file from Cluster
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("DownloadFileFromCluster")]
        [RequestSizeLimit(378)]
        [ProducesResponseType(typeof(IEnumerable<byte>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult DownloadFileFromCluster(DownloadFileFromClusterModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"FileTransfer\" Method: \"DownloadFileFromCluster\" Parameters: \"{model}\"");
                ValidationResult validationResult = new FileTransferValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.DownloadFileFromCluster(model.SubmittedJobInfoId, model.RelativeFilePath, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        #endregion
    }
}
