﻿using HEAppE.BusinessLogicTier.Logic;
using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.DataTransfer;
using HEAppE.ServiceTier.DataTransfer;
using HEAppE.Utils.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HEAppE.RestApi.Controllers
{
    /// <summary>
    /// Data Transfer controller
    /// </summary>
    [ApiController]
    [Route("heappe/[controller]")]
    [Produces("application/json")]
    public class DataTransferController : BaseController<DataTransferController>
    {
        #region Instances
        /// <summary>
        /// Service provider
        /// </summary>
        private readonly IDataTransferService _service = new DataTransferService();
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="memoryCache">Memory cache provider</param>
        public DataTransferController(ILogger<DataTransferController> logger, IMemoryCache memoryCache) : base(logger, memoryCache)
        {

        }
        #endregion
        #region Methods
        /// <summary>
        /// Create Data Transfer
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("RequestDataTransfer")]
        [RequestSizeLimit(170)]
        [ProducesResponseType(typeof(DataTransferMethodExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult RequestDataTransfer(GetDataTransferMethodModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"DataTransfer\" Method: \"RequestDataTransfer\" Parameters: \"{model}\"");
                ValidationResult validationResult = new DataTransferValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(_service.RequestDataTransfer(model.IpAddress, model.Port, model.SubmittedJobInfoId, model.SessionCode));
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
        /// CLose Data Transfer
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("CloseDataTransfer")]
        [RequestSizeLimit(220)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult CloseDataTransfer(EndDataTransferModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"DataTransfer\" Method: \"CloseDataTransfer\" Parameters: \"{model}\"");
                ValidationResult validationResult = new DataTransferValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                _service.CloseDataTransfer(model.UsedTransferMethod, model.SessionCode);
                return Ok("CloseDataTransfer");
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
        /// Send HTTP GET to Job node
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("HttpGetToJobNode")]
        [RequestSizeLimit(50000)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> HttpGetToJobNodeAsync(HttpGetToJobNodeModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"DataTransfer\" Method: \"HttpGetToJobNode\" Parameters: \"{model}\"");
                ValidationResult validationResult = new DataTransferValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(await _service.HttpGetToJobNodeAsync(model.HttpRequest, model.HttpHeaders, model.SubmittedJobInfoId, model.NodeIPAddress, model.NodePort, model.SessionCode));
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
        /// Send HTTP POST to Job node
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("HttpPostToJobNode")]
        [RequestSizeLimit(50000)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> HttpPostToJobNodeAsync(HttpPostToJobNodeModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"DataTransfer\" Method: \"HttpPostToJobNode\" Parameters: \"{model}\"");
                ValidationResult validationResult = new DataTransferValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                return Ok(await _service.HttpPostToJobNodeAsync(model.HttpRequest, model.HttpHeaders, model.HttpPayload, model.SubmittedJobInfoId, model.NodeIPAddress, model.NodePort, model.SessionCode));
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
