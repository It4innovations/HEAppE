using HEAppE.Exceptions.External;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.ClusterInformation;
using HEAppE.ServiceTier.ClusterInformation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;

namespace HEAppE.RestApi.Controllers;

/// <summary>
///     Health Endpoint
/// </summary>
[ApiController]
[Route("heappe/[controller]")]
[Produces("application/json")]
public class HealthController : BaseController<HealthController>
{
    #region Instances

    private readonly HealthCheckService _healthCheckService;

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="cacheProvider">Memory cache instance</param>
    /// <param name="healthCheckService">Health check service instance</param>
    public HealthController(ILogger<HealthController> logger, IMemoryCache cacheProvider, HealthCheckService healthCheckService) :
        base(logger, cacheProvider)
    {
        _healthCheckService = healthCheckService;
    }

    #endregion

    #region Methods

    [HttpGet]
    [ProducesResponseType(typeof(HealthExt), StatusCodes.Status200OK)]
    public async Task<ActionResult> Get()
    {
        HealthReport report = await this._healthCheckService.CheckHealthAsync();
        return Ok(HEAppEHealth.DoProcessHealthReport(report));
    }

    #endregion
}