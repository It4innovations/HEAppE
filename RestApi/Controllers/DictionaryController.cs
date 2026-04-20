using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.RestApiModels.Dictionaries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HEAppE.RestApi.Controllers;

/// <summary>
///     Dictionary Endpoint
/// </summary>
[ApiController]
[Route("heappe/[controller]")]
[Produces("application/json")]
[AllowAnonymous]
[ApiExplorerSettings(GroupName = "Dictionary")]
public class DictionaryController : BaseController<DictionaryController>
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="cacheProvider">Memory cache instance</param>
    public DictionaryController(ILogger<DictionaryController> logger, IMemoryCache cacheProvider) : base(logger, cacheProvider)
    {
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Get cluster authentication credentials auth types mapping (External)
    /// </summary>
    /// <returns>List of auth types</returns>
    [HttpGet("GetClusterAuthenticationCredentialsAuthTypes")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetClusterAuthenticationCredentialsAuthTypes()
    {
        return Ok(GetEnumDictionary<ClusterAuthenticationCredentialsAuthTypeExt>());
    }

    /// <summary>
    ///     Get job states mapping (External)
    /// </summary>
    /// <returns>List of job states</returns>
    [HttpGet("GetJobStates")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetJobStates()
    {
        return Ok(GetEnumDictionary<JobStateExt>());
    }

    /// <summary>
    ///     Get task states mapping (External)
    /// </summary>
    /// <returns>List of task states</returns>
    [HttpGet("GetTaskStates")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetTaskStates()
    {
        return Ok(GetEnumDictionary<TaskStateExt>());
    }

    /// <summary>
    ///     Get task priorities mapping (External)
    /// </summary>
    /// <returns>List of task priorities</returns>
    [HttpGet("GetTaskPriorities")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetTaskPriorities()
    {
        return Ok(GetEnumDictionary<TaskPriorityExt>());
    }

    /// <summary>
    ///     Get adaptor user types mapping (External)
    /// </summary>
    /// <returns>List of user types</returns>
    [HttpGet("GetAdaptorUserTypes")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetAdaptorUserTypes()
    {
        return Ok(GetEnumDictionary<AdaptorUserTypeExt>());
    }

    /// <summary>
    ///     Get scheduler types mapping (External)
    /// </summary>
    /// <returns>List of scheduler types</returns>
    [HttpGet("GetSchedulerTypes")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetSchedulerTypes()
    {
        return Ok(GetEnumDictionary<SchedulerTypeExt>());
    }

    /// <summary>
    ///     Get file transfer protocols mapping (External)
    /// </summary>
    /// <returns>List of file transfer protocols</returns>
    [HttpGet("GetFileTransferProtocols")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetFileTransferProtocols()
    {
        return Ok(GetEnumDictionary<FileTransferProtocolExt>());
    }

    private IEnumerable<DictionaryItemModel> GetEnumDictionary<T>() where T : Enum
    {
        string cacheKey = $"Dictionary_{typeof(T).Name}";
        if (!_cacheProvider.TryGetValue(cacheKey, out IEnumerable<DictionaryItemModel> result))
        {
            result = Enum.GetValues(typeof(T))
                .Cast<T>()
                .Select(e => new DictionaryItemModel { Id = Convert.ToInt32(e), Name = e.ToString() })
                .ToList();

            _cacheProvider.Set(cacheKey, result, TimeSpan.FromHours(24));
        }

        return result;
    }

    #endregion
}
