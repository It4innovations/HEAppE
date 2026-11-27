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
using HEAppE.RestUtils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace HEAppE.RestApi.Logging;

public class LogRequestModelFilter : IAsyncActionFilter
{
    private readonly ILogger<LogRequestModelFilter> _logger;

    private static bool IsSensitiveKey(string key) => SensitiveDataRedactor.IsSensitiveKey(key);

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
            Converters = { new SensitiveDictionaryConverterFactory() },
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
            var serviceParameterNames = GetServiceParameterNames(context);
            var argumentsJson = SerializeArgumentsFast(context.ActionArguments, serviceParameterNames);

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
            if (IsSensitiveKey(key))
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

    private static HashSet<string>? GetServiceParameterNames(ActionExecutingContext context)
    {
        HashSet<string>? serviceParams = null;
        foreach (var param in context.ActionDescriptor.Parameters)
        {
            if (param.BindingInfo?.BindingSource == BindingSource.Services)
            {
                serviceParams ??= new HashSet<string>(StringComparer.Ordinal);
                serviceParams.Add(param.Name);
            }
        }
        return serviceParams;
    }

    private static string SerializeArgumentsFast(IDictionary<string, object?> arguments, HashSet<string>? serviceParameterNames)
    {
        if (arguments == null || arguments.Count == 0) return "{}";

        var buffer = new ArrayBufferWriter<byte>(512);
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();

        foreach (var (key, value) in arguments)
        {
            if (value == null) continue;
            if (serviceParameterNames != null && serviceParameterNames.Contains(key)) continue;
            if (IsUnsafeType(value)) continue;

            writer.WritePropertyName(key);
            if (IsSensitiveKey(key))
            {
                writer.WriteStringValue(SensitiveDataRedactor.RedactedValue);
            }
            else
            {
                JsonSerializer.Serialize(writer, value, value.GetType(), JsonOptions);
            }
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
            if (IsSensitiveKey(property.Name))
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
            writer.WriteStringValue(SensitiveDataRedactor.RedactedValue);
        }
    }

    private sealed class SensitiveDictionaryConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType) return false;
            var genericDef = typeToConvert.GetGenericTypeDefinition();
            if (genericDef != typeof(Dictionary<,>) &&
                genericDef != typeof(IDictionary<,>) &&
                genericDef != typeof(IReadOnlyDictionary<,>))
            {
                return false;
            }
            return typeToConvert.GetGenericArguments()[0] == typeof(string);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var valueType = typeToConvert.GetGenericArguments()[1];
            var converterType = typeof(SensitiveDictionaryConverter<,>).MakeGenericType(typeToConvert, valueType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }

    private sealed class SensitiveDictionaryConverter<TDict, TValue> : JsonConverter<TDict>
        where TDict : IEnumerable<KeyValuePair<string, TValue>>
    {
        public override TDict? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => default;

        public override void Write(Utf8JsonWriter writer, TDict value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                if (kvp.Value != null && IsSensitiveKey(kvp.Key))
                {
                    writer.WriteStringValue(SensitiveDataRedactor.RedactedValue);
                }
                else
                {
                    JsonSerializer.Serialize(writer, kvp.Value, options);
                }
            }
            writer.WriteEndObject();
        }
    }
}