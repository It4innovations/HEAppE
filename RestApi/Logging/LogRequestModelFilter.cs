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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.RestApi.Logging;

public class LogRequestModelFilter : IAsyncActionFilter
{
    private readonly ILogger<LogRequestModelFilter> _logger;

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "SessionCode", "Password", "Token", "Secret", "Key", "sessionCode", "password", "token",
        "Authorization", "Cookie", "Set-Cookie", "X-API-Key"
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
        LogRequestDetailsAsync(context);
        await next();
    }

    private void LogRequestDetailsAsync(ActionExecutingContext context)
    {
        try
        {
            var safeArguments = context.ActionArguments
                .Where(kvp => !IsUnsafeType(kvp.Value))
                .ToDictionary(k => k.Key, v => v.Value);

            var serializedArgs = JsonSerializer.Serialize(safeArguments, _jsonOptions);

            var safeHeaders = ExtractSafeHeaders(context.HttpContext.Request.Headers);
            var serializedHeaders = JsonSerializer.Serialize(safeHeaders, _jsonOptions);

            _logger.LogInformation(
                "Action: {Action}, Headers: {Headers}, Arguments: {Arguments}",
                context.ActionDescriptor.DisplayName, serializedHeaders, serializedArgs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log request information for action {Action}",
                context.ActionDescriptor.DisplayName);
        }
    }

    private Dictionary<string, string> ExtractSafeHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string>();

        foreach (var header in headers)
        {
            if (SensitiveKeys.Contains(header.Key))
            {
                result.Add(header.Key, "***REDACTED***");
            }
            else
            {
                result.Add(header.Key, header.Value.ToString());
            }
        }

        return result;
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