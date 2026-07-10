using System;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.Services.UserOrg;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HEAppE.RestApi.Controllers;

[ApiController]
[Route("heappe/[controller]")]
public class EventsController : Controller
{
    private readonly IHEAppEEventHub _eventHub;
    private readonly IUserOrgService _userOrgService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IUserAndLimitationManagementLogic _authLogic;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IHEAppEEventHub eventHub,
        IUserOrgService userOrgService,
        IHttpContextKeys httpContextKeys,
        IUserAndLimitationManagementLogic authLogic,
        ILogger<EventsController> logger)
    {
        _eventHub = eventHub;
        _userOrgService = userOrgService;
        _httpContextKeys = httpContextKeys;
        _authLogic = authLogic;
        _logger = logger;
    }

    [HttpGet]
    public async Task Get()
    {
        var context = HttpContext;

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
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
            string sessionCode = context.Request.Query["sessionCode"];
            if (string.IsNullOrEmpty(sessionCode))
            {
                sessionCode = context.Request.Headers["SessionCode"];
            }

            if (!string.IsNullOrEmpty(sessionCode))
            {
                try
                {
                    var user = _authLogic.GetUserForSessionCode(sessionCode);
                    if (user != null)
                    {
                        userId = user.Id;
                        _httpContextKeys.Context.AdaptorUserId = user.Id;
                        _httpContextKeys.Context.UserName = user.Username;
                        _httpContextKeys.Context.Email = user.Email;
                        _logger.LogInformation($"[EventsController] User {userId} ({user.Username}) authenticated via SessionCode for WebSocket handshake.");
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
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

        if (_eventHub is RestApi.Events.HEAppEEventHub hub)
        {
            await hub.HandleConnectionAsync(webSocket, context, userId);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
}
