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
            var jobId = await ExtractJobId(context);

            LoggingUtils.AddUserPropertiesToLogThreadContext(userId, userName, email);
            if (jobId.HasValue)
            {
                LoggingUtils.AddJobIdToLogThreadContext(jobId.Value);
            }

            try
            {
                await _next(context);
            }
            finally
            {
                LoggingUtils.RemoveUserPropertiesFromLogThreadContext();
                LoggingUtils.RemoveJobIdFromLogThreadContext();
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
                // get user from http context keys
                // they are filled by LocalAuthenticationHandler or LexisAuthMiddleware depends on authentication type
                userId = keys.Context.AdaptorUserId;
                userName = keys.Context.UserName;
                email = keys.Context.Email;
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

        private static async Task<long?> ExtractJobId(HttpContext context)
        {
            var possibleKeys = new[] { "JobId", "SubmittedJobInfoId", "CreatedJobInfoId", "jobId", "submittedJobInfoId", "createdJobInfoId" };
            foreach (var key in possibleKeys)
            {
                if (context.Request.Query.TryGetValue(key, out var queryValues) && long.TryParse(queryValues.FirstOrDefault(), out var id))
                {
                    return id;
                }
            }
            
            foreach (var key in possibleKeys)
            {
                if (context.Request.RouteValues.TryGetValue(key, out var routeVal) && routeVal != null)
                {
                    if (long.TryParse(routeVal.ToString(), out var id))
                    {
                        return id;
                    }
                }
            }
            
            if (context.Request.ContentLength > 0 && (context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true))
            {
                context.Request.EnableBuffering();
                var position = context.Request.Body.Position;
                context.Request.Body.Position = 0;

                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = position;

                try
                {
                    var json = JsonDocument.Parse(body);
                    foreach (var key in possibleKeys)
                    {
                        if (json.RootElement.TryGetProperty(key, out var prop))
                        {
                            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var id))
                            {
                                return id;
                            }
                            if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out var strId))
                            {
                                return strId;
                            }
                        }
                    }
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
    }
}