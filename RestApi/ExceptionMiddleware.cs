using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.Exceptions.AbstractTypes;
using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Internal;
using HEAppE.Exceptions.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace HEAppE.RestApi;

/// <summary>
///     Request handling
/// </summary>
public class ExceptionMiddleware
{
    #region Constructor

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="next">Request delegate</param>
    /// <param name="logger">Logger</param>
    /// <param name="exceptionsLocalizer"></param>
    /// <param name="configuration"></param>
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger,
        IStringLocalizer<ExceptionsMessages> exceptionsLocalizer, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _exceptionsLocalizer = exceptionsLocalizer;
        _configuration = configuration;
        var redactingRegex = _configuration["ClusterManagementLogMessageRedactingRegex"];
        _redactingRegex = new Regex(redactingRegex ?? "a^", RegexOptions.Compiled);
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Processing http request
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleException(context, ex);
        }
    }

    #endregion

    #region Instances

    private const string Redacted = "*REDACTED*";

    private string _redactingRegexPattern { get; set; } =
        @"(/[^ ]+)|(\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b)";

    private Regex _redactingRegex { get; }

    /// <summary>
    ///     Default Culture
    /// </summary>
    private static readonly CultureInfo _defaultCultureInfo = new("en");

    /// <summary>
    ///     Request delegate
    /// </summary>
    private readonly RequestDelegate _next;

    /// <summary>
    ///     Logger
    /// </summary>
    private readonly ILogger<ExceptionMiddleware> _logger;

    /// <summary>
    ///     Resource localizer
    /// </summary>
    private readonly IStringLocalizer<ExceptionsMessages> _exceptionsLocalizer;

    private readonly IConfiguration _configuration;

    #endregion

    #region Private Methods

    /// <summary>
    ///     Exception handling change Status code
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="exception">Exception</param>
    private async Task HandleException(HttpContext context, Exception exception)
    {
        ProblemDetails problem = new()
        {
            Status = StatusCodes.Status500InternalServerError
        };

        var logLevel = LogLevel.Error;
        switch (exception)
        {
            case InputValidationException:
            case RequestedObjectDoesNotExistException:
                problem.Title = "Validation Problem";
                problem.Detail = GetExceptionMessage(exception);
                problem.Status = StatusCodes.Status404NotFound;
                logLevel = LogLevel.Warning;
                break;
            case SessionCodeNotValidException:
            case AdaptorUserNotAuthorizedForJobException:    
                problem.Title = "Session Code Authentication Problem";
                problem.Detail = GetExceptionMessage(exception);
                problem.Status = StatusCodes.Status401Unauthorized;
                logLevel = LogLevel.Warning;
                break;
            case AuthenticationTypeException:
                problem.Title = "Authentication Problem";
                problem.Detail = GetExceptionMessage(exception);
                problem.Status = exception.Message is "InvalidToken" or
                    "Expired" or
                    "NotPresent" or "IntrospectionTokenNotValid"
                    ? StatusCodes.Status401Unauthorized
                    : StatusCodes.Status500InternalServerError;
                logLevel = LogLevel.Warning;
                break;
            case SlurmException slurmException:
                problem.Title = "Slurm Problem";
                problem.Detail = string.IsNullOrEmpty(slurmException.CommandError)
                    ? GetExceptionMessage(exception)
                    : RedactErrorMessage(slurmException.CommandError);
                problem.Status = StatusCodes.Status502BadGateway;
                break;
            case PbsException pbsException:
                problem.Title = "Pbs Problem";
                problem.Detail = string.IsNullOrEmpty(pbsException.CommandError)
                    ? GetExceptionMessage(exception)
                    : RedactErrorMessage(pbsException.CommandError);
                problem.Status = StatusCodes.Status502BadGateway;
                break;
            case InvalidRequestException:
            case UnableToCreateConnectionException:
                problem.Title = "Invalid Request";
                problem.Detail = GetExceptionMessage(exception);
                problem.Status = StatusCodes.Status400BadRequest;
                break;
            case InternalException:
                problem.Title = "Problem";
                problem.Detail = _exceptionsLocalizer["InternalException"];
                break;
            case NotAllowedException:
                problem.Title = "Not Allowed";
                problem.Detail = GetExceptionMessage(exception);
                problem.Status = StatusCodes.Status403Forbidden;
                break;
            case ExternalException:
                problem.Title = "External Problem";
                problem.Detail = GetExceptionMessage(exception);
                break;
            case BadHttpRequestException badReqException:
                problem.Title = badReqException.Message;
                problem.Status = badReqException.Message switch
                {
                    "Request body too large." => StatusCodes.Status413PayloadTooLarge,
                    "Not found." => StatusCodes.Status404NotFound,
                    _ => StatusCodes.Status400BadRequest
                };
                break;
            case UnauthorizedAccessException:
                problem.Title = "Unauthorized Access";
                problem.Detail = GetExceptionMessage(exception);
                problem.Status = StatusCodes.Status401Unauthorized;
                logLevel = LogLevel.Warning;
                break;
            default:
                problem.Title = "Problem";
                problem.Detail = _exceptionsLocalizer["InternalException"];
                break;
        }

        // Log exception with default 'en' culture localization
        _logger.Log(logLevel, exception, GetExceptionMessage(exception, _defaultCultureInfo));

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = problem.Status.Value;
        await context.Response.WriteAsJsonAsync(problem);
    }


    private string RedactErrorMessage(string exceptionCommandError)
    {
        return _redactingRegex.Replace(exceptionCommandError, Redacted);
    }


    /// <summary>
    ///     Get exception message from resources based on exception type and message
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="localizationCulture">Optional parameter to change localization culture</param>
    /// <returns></returns>
    private string GetExceptionMessage(Exception exception, CultureInfo localizationCulture = null)
    {
        StringBuilder localizedMessage = new();

        // Localize exception message in language from localizationCulture parameter if set
        if (localizationCulture is not null)
        {
            var currentCultureInfo = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = localizationCulture;
            Thread.CurrentThread.CurrentUICulture = localizationCulture;
            FormatExceptionMessage(exception, localizedMessage);
            Thread.CurrentThread.CurrentCulture = currentCultureInfo;
            Thread.CurrentThread.CurrentUICulture = currentCultureInfo;
        }
        else
        {
            FormatExceptionMessage(exception, localizedMessage);
        }

        return localizedMessage.ToString();
    }

    /// <summary>
    ///     Recursively format localized exception messages
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="builder"></param>
    private void FormatExceptionMessage(Exception exception, StringBuilder builder)
    {
        var exceptionName = $"{exception.GetType().Name}_{exception.Message}";
        var localizedException = exception switch
        {
            BaseException baseException when baseException.Args is not null => _exceptionsLocalizer.GetString(
                exceptionName, baseException.Args),
            BaseException => _exceptionsLocalizer.GetString(exceptionName),
            _ => exception.Message
        };

        var message = localizedException == exceptionName ? exception.Message : localizedException;
        builder.Append(message);

        if (exception.InnerException is not null)
            FormatExceptionMessage(exception.InnerException, builder);
        else
            return;
    }

    #endregion
}