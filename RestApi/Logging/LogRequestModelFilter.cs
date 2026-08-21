#pragma warning disable CS8600, CS8602, CS8603, CS8604, CS8625, CS8632
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace HEAppE.RestApi.Logging;

public class LogRequestModelFilter : IAsyncActionFilter
{
    private readonly ILogger<LogRequestModelFilter> _logger;

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "SessionCode", "Password", "Token", "Secret", "Key", "sessionCode", "password", "token",
        "Authorization", "Cookie", "Set-Cookie", "X-API-Key"
    };

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            MaxDepth = 32,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { ConfigureTypeInfo }
            }
        };
    }

    public LogRequestModelFilter(ILogger<LogRequestModelFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        context.HttpContext.Items["LogRequestModelFilter_Executed"] = true;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            LogRequestFast(context);
        }

        await next();
    }

    private void LogRequestFast(ActionExecutingContext context)
    {
        try
        {
            var actionName = context.ActionDescriptor.DisplayName ?? "UnknownAction";
            var headersJson = SerializeHeadersFast(context.HttpContext.Request.Headers);
            var argumentsJson = SerializeArgumentsFast(context.ActionArguments);

            _logger.LogInformation("Action: {Action}, Headers: {Headers}, Arguments: {Arguments}",
                actionName, headersJson, argumentsJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log request information for action {Action}",
                context.ActionDescriptor.DisplayName);
        }
    }

    private static string SerializeHeadersFast(IHeaderDictionary headers)
    {
        if (headers == null || headers.Count == 0) return "{}";

        var buffer = new ArrayBufferWriter<byte>(256);
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();

        foreach (var (key, value) in headers)
        {
            if (SensitiveKeys.Contains(key))
            {
                writer.WriteString(key, "***REDACTED***");
            }
            else
            {
                writer.WriteString(key, value.ToString());
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string SerializeArgumentsFast(IDictionary<string, object?> arguments)
    {
        if (arguments == null || arguments.Count == 0) return "{}";

        var buffer = new ArrayBufferWriter<byte>(512);
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();

        foreach (var (key, value) in arguments)
        {
            if (value == null) continue;
            if (IsUnsafeType(value)) continue;

            writer.WritePropertyName(key);
            JsonSerializer.Serialize(writer, value, value.GetType(), JsonOptions);
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void ConfigureTypeInfo(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

        foreach (var property in typeInfo.Properties)
        {
            if (SensitiveKeys.Contains(property.Name))
            {
                property.CustomConverter = new StaticMaskConverter();
            }
            else if (typeof(CancellationToken).IsAssignableFrom(property.PropertyType) ||
                     typeof(Stream).IsAssignableFrom(property.PropertyType) ||
                     typeof(Delegate).IsAssignableFrom(property.PropertyType))
            {
                property.ShouldSerialize = static (_, _) => false;
            }
        }
    }

    private static bool IsUnsafeType(object? value)
    {
        if (value == null) return false;
        if (value is CancellationToken) return true;
        if (value is Stream) return true;
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