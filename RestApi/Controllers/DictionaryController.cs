using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.JobReporting.Models;
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

    /// <summary>
    ///     Get proxy types mapping (External)
    /// </summary>
    /// <returns>List of proxy types</returns>
    [HttpGet("GetProxyTypes")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetProxyTypes()
    {
        return Ok(GetEnumDictionary<ProxyTypeExt>());
    }

    /// <summary>
    ///     Get database backup types mapping (External)
    /// </summary>
    /// <returns>List of database backup types</returns>
    [HttpGet("GetDatabaseBackupTypes")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetDatabaseBackupTypes()
    {
        return Ok(GetEnumDictionary<DatabaseBackupTypeExt>());
    }

    /// <summary>
    ///     Get cluster connection protocols mapping (External)
    /// </summary>
    /// <returns>List of cluster connection protocols</returns>
    [HttpGet("GetClusterConnectionProtocols")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetClusterConnectionProtocols()
    {
        return Ok(GetEnumDictionary<ClusterConnectionProtocolExt>());
    }

    /// <summary>
    ///     Get accounting state types mapping (External)
    /// </summary>
    /// <returns>List of accounting state types</returns>
    [HttpGet("GetAccountingStateTypes")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetAccountingStateTypes()
    {
        return Ok(GetEnumDictionary<AccountingStateTypeExt>());
    }

    /// <summary>
    ///     Get resource allocation types mapping (External)
    /// </summary>
    /// <returns>List of resource allocation types</returns>
    [HttpGet("GetResourceAllocationTypes")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetResourceAllocationTypes()
    {
        return Ok(GetEnumDictionary<ResourceAllocationTypeExt>());
    }

    /// <summary>
    ///     Get deployment types mapping (External)
    /// </summary>
    /// <returns>List of deployment types</returns>
    [HttpGet("GetDeploymentTypes")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetDeploymentTypes()
    {
        return Ok(GetEnumDictionary<DeploymentTypeExt>());
    }

    /// <summary>
    ///     Get usage types mapping (External)
    /// </summary>
    /// <returns>List of usage types</returns>
    [HttpGet("GetUsageTypes")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetUsageTypes()
    {
        return Ok(GetEnumDictionary<UsageTypeExt>());
    }

    /// <summary>
    ///     Get synchronizable files mapping (External)
    /// </summary>
    /// <returns>List of synchronizable files</returns>
    [HttpGet("GetSynchronizableFiles")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetSynchronizableFiles()
    {
        return Ok(GetEnumDictionary<SynchronizableFilesExt>());
    }

    /// <summary>
    ///     Get file transfer cipher types mapping (External)
    /// </summary>
    /// <returns>List of file transfer cipher types</returns>
    [HttpGet("GetFileTransferCipherTypes")]
    [ProducesResponseType(typeof(IEnumerable<DictionaryItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetFileTransferCipherTypes()
    {
        return Ok(GetEnumDictionary<FileTransferCipherTypeExt>());
    }

    /// <summary>
    ///     Get cluster custom configuration keys mapping with scheduler type compatibility flags (External)
    /// </summary>
    /// <returns>List of cluster custom configuration keys and supported scheduler types</returns>
    [HttpGet("GetClusterCustomConfigurationKeys")]
    [ProducesResponseType(typeof(IEnumerable<ClusterCustomConfigurationKeyItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult GetClusterCustomConfigurationKeys()
    {
        string cacheKey = "Dictionary_ClusterCustomConfigurationKeys_WithSchedulerTypes";
        if (!_cacheProvider.TryGetValue(cacheKey, out IEnumerable<ClusterCustomConfigurationKeyItemModel> result))
        {
            var sshSchedulers = new List<SchedulerTypeExt>
            {
                SchedulerTypeExt.LinuxLocal,
                SchedulerTypeExt.PbsPro,
                SchedulerTypeExt.Slurm,
                SchedulerTypeExt.HyperQueue
            };

            var firecrestSchedulers = new List<SchedulerTypeExt>
            {
                SchedulerTypeExt.FirecRestSlurm
            };

            var allSchedulers = new List<SchedulerTypeExt>
            {
                SchedulerTypeExt.LinuxLocal,
                SchedulerTypeExt.PbsPro,
                SchedulerTypeExt.Slurm,
                SchedulerTypeExt.HyperQueue,
                SchedulerTypeExt.FirecRestSlurm
            };

            var keysMap = new Dictionary<string, List<SchedulerTypeExt>>(StringComparer.OrdinalIgnoreCase);

            // Reflect all properties from ScriptsConfiguration
            foreach (var prop in typeof(HEAppE.HpcConnectionFramework.Configuration.ScriptsConfiguration).GetProperties())
            {
                if (prop.Name.Equals("SshCommandPrefix", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Equals("SyncScriptsViaSftp", StringComparison.OrdinalIgnoreCase))
                {
                    keysMap[prop.Name] = sshSchedulers;
                }
                else
                {
                    keysMap[prop.Name] = allSchedulers;
                }
            }

            // Include additional known metadata keys
            keysMap["IdpUrl"] = allSchedulers;
            keysMap["FirecrestUrl"] = firecrestSchedulers;
            keysMap["ClientId"] = firecrestSchedulers;
            keysMap["ClientSecret"] = firecrestSchedulers;
            keysMap["ClusterName"] = firecrestSchedulers;

            int id = 1;
            result = keysMap
                .OrderBy(k => k.Key)
                .Select(k => new ClusterCustomConfigurationKeyItemModel
                {
                    Id = id++,
                    Name = k.Key,
                    SupportedSchedulerTypes = k.Value.Select(s => new DictionaryItemModel
                    {
                        Id = (int)s,
                        Name = s.ToString()
                    }).ToList()
                })
                .ToList();

            _cacheProvider.Set(cacheKey, result, TimeSpan.FromHours(24));
        }

        return Ok(result);
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
