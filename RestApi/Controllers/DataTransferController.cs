using System;
using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.RestApiModels.DataTransfer;
using HEAppE.ServiceTier.DataTransfer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        public DataTransferController(ILogger<DataTransferController> logger) :base(logger)
        {
        }
        #endregion
        #region Methods
        /// <summary>
        /// Create Data Transfer
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("GetDataTransferMethod")]
        [RequestSizeLimit(170)]
        [ProducesResponseType(typeof(DataTransferMethodExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult BeginDataTransfer(GetDataTransferMethodModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"DataTransfer\" Method: \"GetDataTransferMethod\" Parameters: \"{model}\"");
                return Ok(_service.GetDataTransferMethod(model.IpAddress, model.Port, model.SubmittedJobInfoId, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// End Data Transfer
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("EndDataTransfer")]
        [RequestSizeLimit(188)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult EndDataTransfer(EndDataTransferModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"DataTransfer\" Method: \"EndDataTransfer\" Parameters: \"{model}\"");
                _service.EndDataTransfer(model.UsedTransferMethod, model.SessionCode);
                return Ok("EndDataTransfer");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Send HTTP GET to Job node
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("HttpGetToJobNode")]
        [RequestSizeLimit(50000)]
        [ProducesResponseType(typeof(int?), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult HttpGetToJobNode(HttpGetToJobNodeModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"DataTransfer\" Method: \"HttpGetToJobNode\" Parameters: \"{model}\"");
                return Ok(_service.HttpGetToJobNode(model.HttpRequest, model.HttpHeaders, model.SubmittedJobInfoId, model.IpAddress, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Send HTTP POST to Job node
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("HttpPostToJobNode")]
        [RequestSizeLimit(50000)]
        [ProducesResponseType(typeof(int?), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult HttpPostToJobNode(HttpPostToJobNodeModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"DataTransfer\" Method: \"HttpPostToJobNode\" Parameters: \"{model}\"");
                return Ok(_service.HttpPostToJobNode(model.HttpRequest, model.HttpHeaders, model.HttpPayload, model.SubmittedJobInfoId, model.IpAddress, model.SessionCode));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Write Data to Job node
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //[HttpPost("WriteDataToJobNode")]
        //[RequestSizeLimit(50000)]
        //[ProducesResponseType(typeof(int?), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        //[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public IActionResult WriteDataToJobNode(WriteDataToJobNodeModel model)
        //{
        //    try
        //    {
        //        _logger.LogDebug($"Endpoint: \"DataTransfer\" Method: \"WriteDataToJobNode\" Parameters: \"{model}\"");
        //        return Ok(_service.WriteDataToJobNode(model.Data, model.SubmittedJobInfoId, model.IpAddress, model.CloseConnection, model.SessionCode));
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(e.Message);
        //    }
        //}

        ///// <summary>
        ///// Read Data from Job node
        ///// </summary>
        ///// <param name="model"></param>
        ///// <returns></returns>
        //[HttpPost("ReadDataFromJobNode")]
        //[RequestSizeLimit(154)]
        //[ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        //[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public IActionResult ReadDataFromJobNode(ReadDataFromJobNodeModel model)
        //{
        //    try
        //    {
        //        _logger.LogDebug($"Endpoint: \"DataTransfer\" Method: \"ReadDataFromJobNode\" Parameters: \"{model}\"");
        //        return Ok(_service.ReadDataFromJobNode(model.SubmittedJobInfoId, model.IpAddress, model.SessionCode));
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(e.Message);
        //    }
        //}
        #endregion
    }
}
