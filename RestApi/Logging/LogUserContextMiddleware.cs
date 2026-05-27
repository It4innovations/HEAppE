using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using HEAppE.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _cache;

        public LogUserContextMiddleware(RequestDelegate next, ILogger<LogUserContextMiddleware> logger,
            ISshCertificateAuthorityService sshCertificateAuthorityService, IMemoryCache cache)
        {
            _next = next;
            _logger = logger;
            _sshCertificateAuthorityService = sshCertificateAuthorityService;
            _cache = cache;
        }

        public async Task Invoke(HttpContext context, IHttpContextKeys httpContextKeys, IUserOrgService userOrgService, IExpirioService expirioService)
        {
            log4net.LogicalThreadContext.Properties["requestId"] = context.TraceIdentifier;

            ApplyRequestSizeLimit(context);

            var (userId, userName, email) = await ExtractUserInfo(context, httpContextKeys, userOrgService, expirioService);
            var jobId = await ExtractJobId(context);

            if (userId <= 0 && string.IsNullOrEmpty(userName))
            {
                userName = await ExtractUserNameFromContent(context);
            }

            LoggingUtils.AddUserPropertiesToLogThreadContext(userId, userName, email);
            if (jobId.HasValue)
            {
                LoggingUtils.AddJobIdToLogThreadContext(jobId.Value);
            }
            
            log4net.LogicalThreadContext.Properties["isUserAction"] = true;

            try
            {
                await _next(context);
            }
            finally
            {
                LoggingUtils.RemoveUserPropertiesFromLogThreadContext();
                LoggingUtils.RemoveJobIdFromLogThreadContext();
                log4net.LogicalThreadContext.Properties.Remove("isUserAction");
                log4net.LogicalThreadContext.Properties.Remove("requestId");
            }
        }

        private static void ApplyRequestSizeLimit(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint == null) return;

            // Use reflection to avoid compile-time dependency on Metadata/Features interfaces
            var metadata = endpoint.Metadata;
            var sizeLimitMetadata = metadata.FirstOrDefault(m => m.GetType().GetInterface("IRequestSizeLimitMetadata") != null);
            var allowLargeBodyMetadata = metadata.FirstOrDefault(m => m.GetType().GetInterface("IAllowLargeRequestBodyMetadata") != null);

            if (sizeLimitMetadata == null && allowLargeBodyMetadata == null) return;

            var feature = context.Features.FirstOrDefault(f => f.Key.Name == "IHttpRequestBodySizeFeature").Value;
            if (feature == null) return;

            var isReadOnlyProp = feature.GetType().GetProperty("IsReadOnly");
            if (isReadOnlyProp != null && (bool)isReadOnlyProp.GetValue(feature)) return;

            var maxRequestBodySizeProp = feature.GetType().GetProperty("MaxRequestBodySize");
            if (maxRequestBodySizeProp == null) return;

            if (allowLargeBodyMetadata != null)
            {
                maxRequestBodySizeProp.SetValue(feature, null);
            }
            else if (sizeLimitMetadata != null)
            {
                var limitProp = sizeLimitMetadata.GetType().GetProperty("MaxRequestBodySize");
                if (limitProp != null)
                {
                    maxRequestBodySizeProp.SetValue(feature, limitProp.GetValue(sizeLimitMetadata));
                }
            }
        }

        private async Task<(long userId, string userName, string email)> ExtractUserInfo(HttpContext context, IHttpContextKeys keys, IUserOrgService userOrg, IExpirioService expirioService)
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
                string cacheKey = $"SessionUserInfo_{sessionCode}";
                if (!_cache.TryGetValue(cacheKey, out (long userId, string userName, string email) userInfo))
                {
                    userInfo = await Task.Run(() => GetUserInfo(sessionCode, keys, userOrg, expirioService));
                    var cacheDuration = userInfo.userId > 0 ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(30);
                    _cache.Set(cacheKey, userInfo, cacheDuration);
                }
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

        private (long userId, string userName, string email) GetUserInfo(string sessionCode, IHttpContextKeys keys, IUserOrgService userOrg, IExpirioService expirioService)
        {
            try
            {
                using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger);
                var logic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(
                    unitOfWork, userOrg, _sshCertificateAuthorityService, keys, expirioService, _logger);

                var loggedUser = logic.GetUserForSessionCode(sessionCode);

                return (loggedUser?.Id ?? -1, loggedUser?.Username ?? null, loggedUser?.Email ?? null);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to retrieve user information for session code");
                return (-1, null, null);
            }
        }

        private async Task<string> ExtractUserNameFromContent(HttpContext context)
        {
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
                    // Look for Username in generic credentials structure
                    if (json.RootElement.TryGetProperty("Credentials", out var creds) || json.RootElement.TryGetProperty("credentials", out creds))
                    {
                        if (creds.TryGetProperty("Username", out var user) || creds.TryGetProperty("username", out user))
                            return user.GetString();
                    }
                    // Direct Username property
                    if (json.RootElement.TryGetProperty("Username", out var directUser) || json.RootElement.TryGetProperty("username", out directUser))
                        return directUser.GetString();
                }
                catch { }
            }
            return null;
        }
    }
}