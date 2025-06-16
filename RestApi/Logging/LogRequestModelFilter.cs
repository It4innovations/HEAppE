using System;

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
            
            //anonymize model
            var modelType = model.GetType();
            if(modelType.Name.StartsWith("Authenticate"))
            {
                //do not log sensitive models
                _logger.LogInformation("Action: {Action}, Argument: {Name}, Payload: ***REDACTED***",
                    context.ActionDescriptor.DisplayName,
                    argument.Key);
                continue;
            }
            
            var clone = CloneAndRedact(model, modelType);
            var serialized = JsonSerializer.Serialize(clone);
            _logger.LogInformation("Action: {Action}, Argument: {Name}, Payload: {Payload}",
                context.ActionDescriptor.DisplayName,
                argument.Key,
                serialized);
        }

        base.OnActionExecuting(context);
    }

    private object CloneAndRedact(object model, Type type)
    {
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
}
