using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SshCaAPI;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HEAppE.RestApi.Logging
{
    /// <summary>
    ///     Middleware to append global user properties to logs
    /// </summary>
    public class LogUserContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LogUserContextMiddleware> _logger;
        private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
        private readonly IHttpContextKeys _httpContextKeys;
        private readonly IUserOrgService _userOrgService;

        public LogUserContextMiddleware(RequestDelegate next, ILogger<LogUserContextMiddleware> logger,
            ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys, IUserOrgService userOrgService)
        {
            _next = next;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sshCertificateAuthorityService = sshCertificateAuthorityService ?? throw new ArgumentNullException(nameof(sshCertificateAuthorityService));
            _httpContextKeys = httpContextKeys ?? throw new ArgumentNullException(nameof(httpContextKeys));
            _userOrgService = userOrgService;
        }

        public async Task Invoke(HttpContext context)
        {
            var (userId, userName, email) = await ExtractUserInfo(context);

            // Append log custom user properties
            if (userId > 0)
                log4net.LogicalThreadContext.Properties["userId"] = userId;
            if (!string.IsNullOrEmpty(userName))
                log4net.LogicalThreadContext.Properties["userName"] = userName;
            if (!string.IsNullOrEmpty(email))
                log4net.LogicalThreadContext.Properties["userEmail"] = email;

            try
            {
                await _next(context);
            }
            finally
            {
                log4net.LogicalThreadContext.Properties.Remove("userId");
                log4net.LogicalThreadContext.Properties.Remove("userName");
                log4net.LogicalThreadContext.Properties.Remove("userEmail");
            }
        }

        private async Task<(long userId, string userName, string email)> ExtractUserInfo(HttpContext context)
        {
            var sessionCode = await ExtractSessionCode(context);

            long userId = 0;
            string userName = null;
            string email = null;

            if (string.IsNullOrEmpty(sessionCode))
            {
                if (TryGetFromContext(context, out var ctxId, out var ctxName, out var ctxEmail))
                {
                    userId = ctxId;
                    userName = ctxName;
                    email = ctxEmail;
                }
            }
            else
            {
                var userInfo = await Task.Run(() => GetUserInfo(sessionCode));
                userId = userInfo.userId;
                userName = userInfo.userName;
                email = userInfo.email;
            }

            return (userId, userName, email);
        }

        private static async Task<string> ExtractSessionCode(HttpContext context)
        {
            // Try extract session code from request
            // Query
            var sessionCode = context.Request.Query["SessionCode"].FirstOrDefault();
            if (!string.IsNullOrEmpty(sessionCode))
                return sessionCode;

            // Body
            if (context.Request.ContentLength > 0)
            {
                context.Request.EnableBuffering();

                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                var json = JsonDocument.Parse(body);

                if (json.RootElement.TryGetProperty("SessionCode", out var prop))
                    return prop.GetString();
            }

            return null;
        }

        private (long userId, string userName, string email) GetUserInfo(string sessionCode)
        {
            try
            {
                using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
                var logic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(
                    unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
                var loggedUser = logic.GetUserForSessionCode(sessionCode);

                return (loggedUser?.Id ?? -1, loggedUser?.Username ?? null, loggedUser?.Email ?? null);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to retrieve user information for session code");
                return (-1, null, null);
            }
        }

        private bool TryGetFromContext(HttpContext context, out long userId, out string userName, out string email)
        {
            userName = null;
            email = null;
            userId = -1;

            if (context.Items.TryGetValue("X-API-Key", out var contextItem))
            {
                var apiKey = contextItem?.ToString();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    var parts = apiKey.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        userName = parts[0];
                        userId = -1;
                        email = null;
                        // TODO - implement extracting email
                        // email = parts[1];
                        return true;
                    }
                }
            }
            else if (context.Items.TryGetValue("Authorization", out var item))
            {
                var bearer = item?.ToString();
                if (!string.IsNullOrEmpty(bearer) && bearer.StartsWith("Bearer "))
                {
                    userName = "BEARER AUTH IN HEADER";
                    userId = -1;
                    email = null;
                    return true;
                }
            }

            return false;
        }
    }
}
