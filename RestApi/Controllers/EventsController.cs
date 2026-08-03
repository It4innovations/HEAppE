using System;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.ExtModels.Events.Models;
using HEAppE.Services.UserOrg;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.Services.Expirio;
using SshCaAPI;

namespace HEAppE.RestApi.Controllers;

/// <summary>
///     Events WebSocket Endpoint
/// </summary>
[ApiController]
[Route("heappe/[controller]")]
[Produces("application/json")]
public class EventsController : Controller
{
    private readonly IHEAppEEventHub _eventHub;
    private readonly IUserOrgService _userOrgService;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IExpirioService _expirioService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IHEAppEEventHub eventHub,
        IUserOrgService userOrgService,
        ISshCertificateAuthorityService sshCertificateAuthorityService,
        IExpirioService expirioService,
        IHttpContextKeys httpContextKeys,
        ILogger<EventsController> logger)
    {
        _eventHub = eventHub;
        _userOrgService = userOrgService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _expirioService = expirioService;
        _httpContextKeys = httpContextKeys;
        _logger = logger;
    }

    /// <summary>
    ///     Establish a WebSocket connection for real-time CloudEvents (v1.0) streaming.
    /// </summary>
    /// <param name="sessionCode">Optional session code for authentication via query parameter.</param>
    /// <response code="101">Switching Protocols - WebSocket connection established for streaming CloudEvents.</response>
    /// <response code="200">OK - Returns CloudEvent stream schema representation.</response>
    /// <response code="400">Bad Request - Request is not a valid WebSocket request.</response>
    /// <response code="401">Unauthorized - Authentication failed or missing credentials.</response>
    /// <response code="500">Internal Server Error - Server failure during event hub processing.</response>
    [HttpGet]
    [ProducesResponseType(typeof(CloudEventExt), StatusCodes.Status101SwitchingProtocols)]
    [ProducesResponseType(typeof(CloudEventExt), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get([FromQuery] string sessionCode = null)
    {
        var context = HttpContext;

        if (!context.WebSockets.IsWebSocketRequest)
        {
            return BadRequest();
        }

        long userId = -1;

        // 1. Check if authenticated via headers (X-API-Key / bearer token)
        if (_httpContextKeys.Context.AdaptorUserId > 0)
        {
            userId = _httpContextKeys.Context.AdaptorUserId;
            string authMethod = context.Request.Headers.ContainsKey("X-API-Key") ? "X-API-Key" : "Authorization (Bearer)";
            _logger.LogInformation($"[EventsController] User {userId} authenticated via {authMethod} for WebSocket handshake.");
        }
        else
        {
            // 2. Fallback to legacy SessionCode (query param or header)
            if (string.IsNullOrEmpty(sessionCode))
            {
                sessionCode = context.Request.Headers["SessionCode"];
            }

            if (!string.IsNullOrEmpty(sessionCode))
            {
                try
                {
                    using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
                    {
                        var authLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(
                            unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);

                        var user = authLogic.GetUserForSessionCode(sessionCode);
                        if (user != null)
                        {
                            userId = user.Id;
                            _httpContextKeys.Context.AdaptorUserId = user.Id;
                            _httpContextKeys.Context.UserName = user.Username;
                            _httpContextKeys.Context.Email = user.Email;
                            _logger.LogInformation($"[EventsController] User {userId} ({user.Username}) authenticated via SessionCode for WebSocket handshake.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[EventsController] SessionCode authentication failed: {ex.Message}");
                }
            }
        }

        if (userId <= 0)
        {
            _logger.LogWarning("[EventsController] WebSocket handshake rejected: Unauthorized.");
            return Unauthorized();
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

        if (_eventHub is RestApi.Events.HEAppEEventHub hub)
        {
            await hub.HandleConnectionAsync(webSocket, context, userId);
            return new EmptyResult();
        }

        return StatusCode(StatusCodes.Status500InternalServerError);
    }
}
