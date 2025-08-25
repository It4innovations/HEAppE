using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace HEAppE.RestApi.Logging;

public class LogRequestModelFilter : ActionFilterAttribute
{
    private readonly ILogger<LogRequestModelFilter> _logger;
    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "SessionCode", "Password", "Token", "Secret", "Key"
    };

    public LogRequestModelFilter(ILogger<LogRequestModelFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        try
        {
            var (userId, userName) = ExtractUserInfo(context.ActionArguments);
            var argumentsLog = ProcessActionArguments(context.ActionArguments);
            
            LogRequestInfo(context, userId, userName, argumentsLog);
        }
        catch (Exception ex)
        {
            // Log the exception but don't let it break the request pipeline
            _logger.LogWarning(ex, "Failed to log request information for action {Action}", 
                context.ActionDescriptor.DisplayName);
        }
        
        base.OnActionExecuting(context);
    }

    private (long userId, string userName) ExtractUserInfo(IDictionary<string, object> actionArguments)
    {
        foreach (var argument in actionArguments.Values)
        {
            if (argument == null) continue;

            var sessionCode = ExtractSessionCode(argument);
            if (!string.IsNullOrEmpty(sessionCode))
            {
                return GetUserInfo(sessionCode);
            }
        }

        return (0, string.Empty);
    }

    private string ExtractSessionCode(object model)
    {
        // Try to get SessionCode property
        var sessionCodeProperty = model.GetType().GetProperty("SessionCode");
        if (sessionCodeProperty?.GetValue(model) is string sessionCode && !string.IsNullOrEmpty(sessionCode))
        {
            return sessionCode;
        }

        // If model is a GUID string, treat it as a session code
        if (model is string strModel && Guid.TryParse(strModel, out _))
        {
            return strModel;
        }

        return null;
    }

    private Dictionary<string, object> ProcessActionArguments(IDictionary<string, object> actionArguments)
    {
        var argumentsLog = new Dictionary<string, object>();

        foreach (var argument in actionArguments)
        {
            if (argument.Value == null)
            {
                argumentsLog[argument.Key] = null;
                continue;
            }

            argumentsLog[argument.Key] = ShouldRedactModel(argument.Value) 
                ? "***REDACTED***" 
                : CloneAndRedactSensitiveData(argument.Value);
        }

        return argumentsLog;
    }

    private bool ShouldRedactModel(object model)
    {
        return model.GetType().Name.StartsWith("Authenticate", StringComparison.OrdinalIgnoreCase);
    }

    private object CloneAndRedactSensitiveData(object model)
    {
        try
        {
            var modelType = model.GetType();

            // Handle primitive types and strings
            if (modelType.IsPrimitive || model is string)
            {
                return model is string strModel && Guid.TryParse(strModel, out _) 
                    ? "***REDACTED***" 
                    : model;
            }

            // Handle complex objects
            var copy = Activator.CreateInstance(modelType);
            if (copy == null) return null;

            var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                if (!property.CanRead || !property.CanWrite) continue;

                var value = property.GetValue(model);
                var redactedValue = ShouldRedactProperty(property.Name) 
                    ? "***REDACTED***" 
                    : value;

                property.SetValue(copy, redactedValue);
            }

            return copy;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to clone and redact model of type {ModelType}", model.GetType().Name);
            return "***SERIALIZATION_ERROR***";
        }
    }

    private bool ShouldRedactProperty(string propertyName)
    {
        return SensitiveProperties.Contains(propertyName);
    }

    private void LogRequestInfo(ActionExecutingContext context, long userId, string userName, 
        Dictionary<string, object> argumentsLog)
    {
        try
        {
            var serializedArgs = JsonSerializer.Serialize(argumentsLog, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("Action: {Action}, UserId: {UserId}, UserName: {UserName}, Arguments: {Arguments}",
                context.ActionDescriptor.DisplayName,
                userId,
                userName,
                serializedArgs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize arguments for logging");
        }
    }

    private (long userId, string userName) GetUserInfo(string sessionCode)
    {
        try
        {
            using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
            var authenticationLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork, null);
            var loggedUser = authenticationLogic.GetUserForSessionCode(sessionCode);
            
            return (loggedUser?.Id ?? -1, loggedUser?.Username ?? "Unknown Username");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to retrieve user information for session code");
            return (-1, "Unknown Username");
        }
    }
}