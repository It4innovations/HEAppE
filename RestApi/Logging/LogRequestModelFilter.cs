using System;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.ServiceTier.UserAndLimitationManagement;

namespace HEAppE.RestApi.Logging;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Reflection;

public class LogRequestModelFilter : ActionFilterAttribute
{
    private readonly ILogger<LogRequestModelFilter> _logger;

    public LogRequestModelFilter(ILogger<LogRequestModelFilter> logger)
    {
        _logger = logger;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var argument in context.ActionArguments)
        {
            var model = argument.Value;
            if (model == null) continue;
            long userId = 0;
            string userName = string.Empty;
            
            //get SessionCode property value from the model if available
            var sessionCodeProperty = model.GetType().GetProperty("SessionCode");
            if (sessionCodeProperty != null)
            {
                var sessionCode = sessionCodeProperty.GetValue(model)?.ToString();
                if (!string.IsNullOrEmpty(sessionCode))
                {
                    (userId, userName) = GetUser(sessionCode);
                }
            }
            
            if (sessionCodeProperty == null && Guid.TryParse(model.ToString(), out Guid parsedGuid))
            {
                //if model is a guid then check if it is a session code
                (userId, userName) = GetUser(parsedGuid.ToString());
            }
            
            //anonymize model
            var modelType = model.GetType();
            if(modelType.Name.StartsWith("Authenticate"))
            {
                //do not log sensitive models
                _logger.LogInformation("Action: {Action}, Argument: {Name}, UserId: {UserId}, UserName: {UserName}, Payload: ***REDACTED***",
                    context.ActionDescriptor.DisplayName,
                    argument.Key,
                    userId,
                    userName);
                continue;
            }
            
            var clone = CloneAndRedact(model, modelType);
            var serialized = JsonSerializer.Serialize(clone);
            _logger.LogInformation("Action: {Action}, Argument: {Name}, UserId: {UserId}, UserName: {UserName}, Payload: {Payload}",
                context.ActionDescriptor.DisplayName,
                argument.Key,
                userId,
                userName,
                serialized);
        }

        base.OnActionExecuting(context);
    }

    private object CloneAndRedact(object model, Type type)
    {
        ///if model is string then check if is guid
        if (model is string strModel && Guid.TryParse(strModel, out _))
        {
            return "***REDACTED***";
        }
        var copy = Activator.CreateInstance(type);
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite)
                continue;

            var value = prop.GetValue(model);

            // redact sensitive information
            if (string.Equals(prop.Name, "SessionCode", StringComparison.OrdinalIgnoreCase))
            {
                prop.SetValue(copy, "***REDACTED***");
            }
            else
            {
                prop.SetValue(copy, value);
            }
        }

        return copy!;
    }
    
    private (long, string) GetUser(string sessionCode)
    {
        try
        {
            using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
            var authenticationLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
            var loggedUser = authenticationLogic.GetUserForSessionCode(sessionCode);
            return (loggedUser.Id, loggedUser?.Username ?? "Unknown Username");
        }catch (Exception ex)
        {
            return (-1, "Unknown Username");
        }
        
    }
    
}
