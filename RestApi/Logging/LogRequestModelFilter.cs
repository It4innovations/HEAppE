using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.RestApi.Logging;

public class LogRequestModelFilter : ActionFilterAttribute
{
    private readonly ILogger<LogRequestModelFilter> _logger;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "SessionCode", "Password", "Token", "Secret", "Key"
    };

    // Cache pro SessionCode property, abychom ji nehledali reflexí stále dokola
    private static readonly ConcurrentDictionary<Type, PropertyInfo> SessionCodePropertyCache = new();

    // Nastavení serializace s vlastním konvertorem pro maskování
    private readonly JsonSerializerOptions _jsonOptions;

    public LogRequestModelFilter(ILogger<LogRequestModelFilter> logger, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        _jsonOptions.Converters.Add(new SensitiveDataConverter(SensitiveKeys));
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        try
        {
            var sessionCode = ExtractSessionCode(context.ActionArguments);
            var (userId, userName) = string.IsNullOrEmpty(sessionCode) 
                ? (0L, "Anonymous") 
                : GetUserInfo(sessionCode);
            
            var serializedArgs = JsonSerializer.Serialize(context.ActionArguments, _jsonOptions);

            _logger.LogInformation("Action: {Action}, UserId: {UserId}, UserName: {UserName}, Arguments: {Arguments}",
                context.ActionDescriptor.DisplayName, userId, userName, serializedArgs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log request information for action {Action}", 
                context.ActionDescriptor.DisplayName);
        }

        base.OnActionExecuting(context);
    }

    private string ExtractSessionCode(IDictionary<string, object> actionArguments)
    {
        foreach (var argument in actionArguments.Values)
        {
            if (argument == null) continue;

            if (argument is string strValue && Guid.TryParse(strValue, out _))
                return strValue;

            var type = argument.GetType();
            var prop = SessionCodePropertyCache.GetOrAdd(type, t => 
                t.GetProperty("SessionCode", BindingFlags.Public | BindingFlags.Instance));

            if (prop?.GetValue(argument) is string sessionCode)
                return sessionCode;
        }
        return null;
    }

    private (long userId, string userName) GetUserInfo(string sessionCode)
    {
        try
        {
            using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
            var logic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
            var user = logic.GetUserForSessionCode(sessionCode);
            
            return (user?.Id ?? -1, user?.Username ?? "Unknown");
        }
        catch
        {
            return (-1, "Error retrieving user");
        }
    }

    // --- Vnitřní třída pro efektivní maskování během serializace ---
    private class SensitiveDataConverter : JsonConverter<object>
    {
        private readonly HashSet<string> _sensitiveKeys;
        public SensitiveDataConverter(HashSet<string> sensitiveKeys) => _sensitiveKeys = sensitiveKeys;

        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.IsClass && typeToConvert != typeof(string);

        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        { 
            throw new NotImplementedException("Deserialization is not implemented for SensitiveDataConverter.");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                    continue;

                writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(prop.Name) ?? prop.Name);

                if (_sensitiveKeys.Contains(prop.Name))
                {
                    writer.WriteStringValue("***REDACTED***");
                }
                else
                {
                    var propValue = prop.GetValue(value);
                    JsonSerializer.Serialize(writer, propValue, prop.PropertyType, options);
                }
            }

            writer.WriteEndObject();
        }
    }
}