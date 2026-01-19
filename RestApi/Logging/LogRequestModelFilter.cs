using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.RestApi.Logging;

public class LogRequestModelFilter : IAsyncActionFilter
{
    private readonly ILogger<LogRequestModelFilter> _logger;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IUserOrgService _userOrgService;

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "SessionCode", "Password", "Token", "Secret", "Key", "Heslo"
    };

    private static readonly ConcurrentDictionary<Type, PropertyInfo?> SessionCodePropertyCache = new();

    private readonly JsonSerializerOptions _jsonOptions;

    public LogRequestModelFilter(
        ILogger<LogRequestModelFilter> logger, 
        IUserOrgService userOrgService, 
        ISshCertificateAuthorityService sshCertificateAuthorityService, 
        IHttpContextKeys httpContextKeys)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userOrgService = userOrgService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            MaxDepth = 64,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { MaskSensitiveProperties }
            }
        };
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await LogRequestDetailsAsync(context);
        await next();
    }

    private async Task LogRequestDetailsAsync(ActionExecutingContext context)
    {
        try
        {
            var sessionCode = ExtractSessionCode(context.ActionArguments);
            
            long userId = 0;
            string userName = "Anonymous";

            if (string.IsNullOrEmpty(sessionCode))
            {
                if (TryGetFromContext(context.HttpContext, out var ctxId, out var ctxName))
                {
                    userId = ctxId;
                    userName = ctxName;
                }
            }
            else
            {
                var userInfo = await Task.Run(() => GetUserInfo(sessionCode));
                userId = userInfo.userId;
                userName = userInfo.userName;
            }
            
            var safeArguments = context.ActionArguments
                .Where(kvp => !IsUnsafeType(kvp.Value))
                .ToDictionary(k => k.Key, v => v.Value);

            var serializedArgs = JsonSerializer.Serialize(safeArguments, _jsonOptions);

            _logger.LogInformation(
                "Action: {Action}, UserId: {UserId}, UserInfo: {UserName}, Arguments: {Arguments}",
                context.ActionDescriptor.DisplayName, userId, userName, serializedArgs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log request information for action {Action}", 
                context.ActionDescriptor.DisplayName);
        }
    }

    private bool TryGetFromContext(HttpContext context, out long userId, out string userName)
    {
        userId = 0;
        userName = string.Empty;

        if (context.Items.TryGetValue(_httpContextKeys.Context.AdaptorUserId, out var userIdObj) &&
            context.Items.TryGetValue(_httpContextKeys.Context.UserInfo, out var userNameObj) &&
            userIdObj is long id && userNameObj is string name)
        {
            userId = id;
            userName = name;
            return true;
        }
        return false;
    }

    private string? ExtractSessionCode(IDictionary<string, object?> actionArguments)
    {
        foreach (var argument in actionArguments.Values)
        {
            if (argument == null) continue;
            
            if (argument is string strValue && Guid.TryParse(strValue, out _)) 
                return strValue;

            var type = argument.GetType();
            
            if (type.IsPrimitive || type.IsValueType || type == typeof(string)) continue;

            var prop = SessionCodePropertyCache.GetOrAdd(type, t => 
                t.GetProperty("SessionCode", BindingFlags.Public | BindingFlags.Instance));

            if (prop != null && prop.GetValue(argument) is string sessionCode) 
                return sessionCode;
        }
        return null;
    }

    private (long userId, string userName) GetUserInfo(string sessionCode)
    {
        try
        {
            using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
            var logic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(
                unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
                
            var user = logic.GetUserForSessionCode(sessionCode);
            return (user?.Id ?? -1, user?.Username ?? "Unknown");
        }
        catch
        {
            return (-1, "Error retrieving user");
        }
    }

    private static void MaskSensitiveProperties(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

        foreach (var property in typeInfo.Properties)
        {
            if (SensitiveKeys.Contains(property.Name))
            {
                property.CustomConverter = new StaticMaskConverter();
            }
        }
    }

    private static bool IsUnsafeType(object? value)
    {
        if (value == null) return false;
        if (value is CancellationToken) return true;
        if (value is System.IO.Stream) return true;
        if (value is Delegate) return true;
        return false;
    }

    private class StaticMaskConverter : JsonConverter<object>
    {
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => null;
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteStringValue("***REDACTED***");
        }
    }
}