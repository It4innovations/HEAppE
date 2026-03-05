using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace HEAppE.DataStagingAPI.EndpointFilters;

public class LogRequestEndpointFilter : IEndpointFilter
{
    private readonly ILogger<LogRequestEndpointFilter> _logger;

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "SessionCode", "Password", "Token", "Secret", "Key", "sessionCode", "password", "token",
        "Authorization", "Cookie", "Set-Cookie", "X-API-Key"
    };

    private readonly JsonSerializerOptions _jsonOptions;

    public LogRequestEndpointFilter(ILogger<LogRequestEndpointFilter> logger)
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

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        LogRequestDetails(context);
        return await next(context);
    }

    private void LogRequestDetails(EndpointFilterInvocationContext context)
    {
        try
        {
            var arguments = new Dictionary<string, object?>();
            for (int i = 0; i < context.Arguments.Count; i++)
            {
                var arg = context.Arguments[i];
                if (!IsUnsafeType(arg))
                {
                    arguments.Add($"arg{i}", arg);
                }
            }

            var serializedArgs = JsonSerializer.Serialize(arguments, _jsonOptions);

            var safeHeaders = ExtractSafeHeaders(context.HttpContext.Request.Headers);
            var serializedHeaders = JsonSerializer.Serialize(safeHeaders, _jsonOptions);

            _logger.LogInformation(
                "Endpoint: {Endpoint}, Headers: {Headers}, Arguments: {Arguments}",
                context.HttpContext.Request.Path, serializedHeaders, serializedArgs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log request information for endpoint {Endpoint}",
                context.HttpContext.Request.Path);
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
        if (value is Stream) return true;
        if (value is Delegate) return true;
        if (value is HttpContext) return true;
        if (value is HttpRequest) return true;
        if (value is HttpResponse) return true;
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
