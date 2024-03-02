using FluentValidation;
using HEAppE.Exceptions.AbstractTypes;
using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Text;

namespace HEAppE.DataStagingAPI
{
    /// <summary>
    /// Exception middleware
    /// </summary>
    public class ExceptionMiddleware
    {
        #region Instances
        /// <summary>
        /// Default Culture
        /// </summary>
        private static readonly CultureInfo _defaultCultureInfo = new("en");

        /// <summary>
        /// Request delegate
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<ExceptionMiddleware> _logger;

        /// <summary>
        /// Resource localizer
        /// </summary>
        private readonly IStringLocalizer<ExceptionsMessages> _exceptionsLocalizer;

        #endregion
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="next">Request delegate</param>
        /// <param name="logger">Logger</param>
        /// <param name="exceptionsLocalizer"></param>
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IStringLocalizer<ExceptionsMessages> exceptionsLocalizer)
        {
            _next = next;
            _logger = logger;
            _exceptionsLocalizer = exceptionsLocalizer;
        }
        #endregion

        /// <summary>
        /// Invoke Middleware
        /// </summary>
        /// <param name="context">Context</param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException e)
            {
                ProblemDetails problem = new();
                problem.Title = "Validation Failed";
                problem.Status = StatusCodes.Status400BadRequest;
                problem.Extensions.Add("errors", e.Errors.Select(x => new
                {
                    x.PropertyName,
                    x.ErrorMessage
                }));

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(problem);
            }
            catch (Exception ex)
            {
                await HandleException(context, ex);
            }
        }
        #region Private Methods
        /// <summary>
        /// Exception handling change Status code
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
                    problem.Title = "Validation Problem";
                    problem.Detail = GetExceptionMessage(exception);
                    problem.Status = StatusCodes.Status404NotFound;
                    logLevel = LogLevel.Warning;
                    break;
                case AuthenticationTypeException:
                    problem.Title = "Authentication Problem";
                    problem.Detail = GetExceptionMessage(exception);
                    problem.Status = exception.Message == "InvalidToken" ? StatusCodes.Status401Unauthorized : StatusCodes.Status500InternalServerError;
                    logLevel = LogLevel.Warning;
                    break;
                case InternalException:
                    problem.Title = "Problem";
                    problem.Detail = _exceptionsLocalizer["InternalException"];
                    break;
                case ExternalException:
                    problem.Title = "External Problem";
                    problem.Detail = GetExceptionMessage(exception);
                    break;
                case BadHttpRequestException:
                    var badReqException = (BadHttpRequestException)exception;
                    problem.Title = badReqException.Message;
                    problem.Status = badReqException.Message switch
                    {
                        "Request body too large." => StatusCodes.Status413PayloadTooLarge,
                        "Not found." => StatusCodes.Status404NotFound,
                        _ => StatusCodes.Status400BadRequest
                    };
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

        /// <summary>
        /// Get exception message from resources based on exception type and message
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
        /// Recursively format localized exception messages
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="builder"></param>
        private void FormatExceptionMessage(Exception exception, StringBuilder builder)
        {
            string exceptionName = $"{exception.GetType().Name}_{exception.Message}";
            string localizedException = exception switch
            {
                BaseException baseException when baseException.Args is not null => _exceptionsLocalizer.GetString(exceptionName, baseException.Args),
                BaseException => _exceptionsLocalizer.GetString(exceptionName),
                _ => exception.Message
            };

            var message = localizedException == exceptionName ? exception.Message : localizedException;
            builder.Append(message);

            if (exception.InnerException is not null)
            {
                FormatExceptionMessage(exception.InnerException, builder);
            }
            else
            {
                return;
            }
        }
        #endregion
    }
}
