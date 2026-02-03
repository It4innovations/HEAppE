using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.Services.UserOrg;
using HEAppE.Utils;
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
    public class LogUserContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LogUserContextMiddleware> _logger;
        private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;

        public LogUserContextMiddleware(RequestDelegate next, ILogger<LogUserContextMiddleware> logger,
            ISshCertificateAuthorityService sshCertificateAuthorityService)
        {
            _next = next;
            _logger = logger;
            _sshCertificateAuthorityService = sshCertificateAuthorityService;
        }

        public async Task Invoke(HttpContext context, IHttpContextKeys httpContextKeys, IUserOrgService userOrgService)
        {
            var (userId, userName, email) = await ExtractUserInfo(context, httpContextKeys, userOrgService);

            LoggingUtils.AddUserPropertiesToLogThreadContext(userId, userName, email);

            try
            {
                await _next(context);
            }
            finally
            {
                LoggingUtils.RemoveUserPropertiesFromLogThreadContext();
            }
        }

        private async Task<(long userId, string userName, string email)> ExtractUserInfo(HttpContext context, IHttpContextKeys keys, IUserOrgService userOrg)
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
                var userInfo = await Task.Run(() => GetUserInfo(sessionCode, keys, userOrg));
                userId = userInfo.userId;
                userName = userInfo.userName;
                email = userInfo.email;
            }

            return (userId, userName, email);
        }

        private static async Task<string> ExtractSessionCode(HttpContext context)
        {
            var sessionCode = context.Request.Query["SessionCode"].FirstOrDefault();
            if (!string.IsNullOrEmpty(sessionCode))
                return sessionCode;

            if (context.Request.ContentLength > 0)
            {
                context.Request.EnableBuffering();

                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                try 
                {
                    var json = JsonDocument.Parse(body);
                    if (json.RootElement.TryGetProperty("SessionCode", out var prop))
                        return prop.GetString();
                }
                catch { }
            }

            return null;
        }

        private (long userId, string userName, string email) GetUserInfo(string sessionCode, IHttpContextKeys keys, IUserOrgService userOrg)
        {
            try
            {
                using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
                var logic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(
                    unitOfWork, userOrg, _sshCertificateAuthorityService, keys);
                
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
                    return true;
                }
            }

            return false;
        }
    }
}