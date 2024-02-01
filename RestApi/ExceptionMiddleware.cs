using HEAppE.Exceptions.AbstractTypes;
using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HEAppE.RestApi
{
    /// <summary>
    /// Request handling
    /// </summary>
    public class ExceptionMiddleware
    {
        #region Instances
        /// <summary>
        /// Default Culture
        /// </summary>
        private static readonly CultureInfo _defaultCultureInfo = new CultureInfo("en");

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
        #region Methods
        /// <summary>
        /// Processing http request
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
        #region Private Methods
        /// <summary>
        /// Exception handling change Status code
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="exception"></param>
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
                    problem.Detail = "Problem occured! Contact the administrators.";
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
                    problem.Detail = "Problem occured! Contact the administrators.";
                    break;
            }


            var currentCultureInfo = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = _defaultCultureInfo;
            _logger.Log(logLevel, exception, GetExceptionMessage(exception));
            Thread.CurrentThread.CurrentCulture= currentCultureInfo;

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(problem);
        }

        /// <summary>
        /// Get exception message from resources based on exception type and message
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        private string GetExceptionMessage(Exception exception)
        {
            StringBuilder localizedMessage = new();
            FormatExceptionMessage(exception, localizedMessage);
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
            builder.AppendLine(message);

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
