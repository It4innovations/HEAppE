using System;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.DataTransfer;
using HEAppE.ServiceTier.DataTransfer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.RestApi.Controllers;

/// <summary>
///     Data Transfer controller
/// </summary>
[ApiController]
[Route("heappe/[controller]")]
[Produces("application/json")]
public class DataTransferController : BaseController<DataTransferController>
{
    #region Instances

    /// <summary>
    ///     Service provider
    /// </summary>
    private readonly IDataTransferService _service;

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="memoryCache">Memory cache provider</param>
    public DataTransferController(ILogger<DataTransferController> logger, IMemoryCache memoryCache, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys) : base(logger,
        memoryCache)
    {
        _service = new DataTransferService(sshCertificateAuthorityService, httpContextKeys);
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Create Data Transfer
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("RequestDataTransfer")]
    [RequestSizeLimit(170)]
    [ProducesResponseType(typeof(DataTransferMethodExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult RequestDataTransfer(GetDataTransferMethodModel model)
    {
        _logger.LogDebug($"Endpoint: \"DataTransfer\" Method: \"RequestDataTransfer\" Parameters: \"{model}\"");
        var validationResult = new DataTransferValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(
            _service.RequestDataTransfer(model.IpAddress, model.Port, model.SubmittedTaskInfoId, model.SessionCode));
    }

    /// <summary>
    ///     CLose Data Transfer
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("CloseDataTransfer")]
    [RequestSizeLimit(220)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult CloseDataTransfer(EndDataTransferModel model)
    {
        _logger.LogDebug($"Endpoint: \"DataTransfer\" Method: \"CloseDataTransfer\" Parameters: \"{model}\"");
        var validationResult = new DataTransferValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        _service.CloseDataTransfer(model.UsedTransferMethod, model.SessionCode);
        return Ok("CloseDataTransfer");
    }

    /// <summary>
    ///     Send HTTP GET to Job node
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("HttpGetToJobNode")]
    [RequestSizeLimit(50000)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HttpGetToJobNodeAsync(HttpGetToJobNodeModel model)
    {
        _logger.LogDebug($"Endpoint: \"DataTransfer\" Method: \"HttpGetToJobNode\" Parameters: \"{model}\"");
        var validationResult = new DataTransferValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.HttpGetToJobNodeAsync(model.HttpRequest, model.HttpHeaders, model.SubmittedTaskInfoId,
            model.NodeIPAddress, model.NodePort, model.SessionCode));
    }

    /// <summary>
    ///     Send HTTP POST to Job node
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("HttpPostToJobNode")]
    [RequestSizeLimit(5000000)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HttpPostToJobNodeAsync(HttpPostToJobNodeModel model)
    {
        _logger.LogDebug($"Endpoint: \"DataTransfer\" Method: \"HttpPostToJobNode\" Parameters: \"{model}\"");
        var validationResult = new DataTransferValidator(model).Validate();
        if (!validationResult.IsValid) throw new InputValidationException(validationResult.Message);

        return Ok(await _service.HttpPostToJobNodeAsync(model.HttpRequest, model.HttpHeaders, model.HttpPayload,
            model.SubmittedTaskInfoId, model.NodeIPAddress, model.NodePort, model.SessionCode));
    }
    
    /// <summary>
    /// HttpPostToJobNodeStream
    /// </summary>
    /// <param name="model"></param>
    [HttpPost("HttpPostToJobNodeStream")]
    [RequestSizeLimit(5000000)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task HttpPostToJobNodeStream([FromBody] HttpPostToJobNodeModel model)
    {
        _logger.LogInformation($"Endpoint: \"DataTransfer\" Method: \"HttpPostToJobNodeStream\" Parameters: \"{model}\"");
        
        var validationResult = new DataTransferValidator(model).Validate();
        if (!validationResult.IsValid)
        {
            Response.ContentType = "text/plain";
            _logger.LogInformation("Validation failed: {Message}", validationResult.Message);
            await Response.WriteAsync($"Validation failed: {validationResult.Message}\n");
            await Response.Body.FlushAsync();
            return;
        }
        
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        Response.Headers.Append("Pragma", "no-cache");
        Response.Headers.Append("Expires", "0");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // Nginx
        Response.Headers.Append("X-Content-Type-Options", "nosniff");
        
        try
        {
            // Použití streaming metody místo původní async metody
            await _service.HttpPostToJobNodeStreamAsync(
                model.HttpRequest,
                model.HttpHeaders,
                model.HttpPayload,
                model.SubmittedJobInfoId,
                model.NodeIPAddress,
                model.NodePort,
                model.SessionCode,
                Response.Body,
                HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during HttpPostToJobNodeStream: {Message}", ex.Message);
            await Response.WriteAsync($"data: Error: {ex.Message}\n\n");
            await Response.Body.FlushAsync();
        }
    }


    #endregion
}