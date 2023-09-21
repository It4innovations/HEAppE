using Exceptions.External;
using Exceptions.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Exceptions.Resources;
using Exceptions.Base;
using System.Text;

namespace HEAppE.RestApi
{
    /// <summary>
    /// Request handling
    /// </summary>
    public class ExceptionMiddleware
    {
        #region Instances
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
            string message = string.Empty;

            switch (exception)
            {
                case InputValidationException:
                    message = GetExceptionMessage(exception);
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case InternalException:
                    message = GetExceptionMessage(exception);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
                case ExternalException:
                    message = GetExceptionMessage(exception);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
                case BadHttpRequestException:
                    {
                        var badReqException = (BadHttpRequestException)exception;
                        context.Response.StatusCode = badReqException.Message switch
                        {
                            "Request body too large." => (int)HttpStatusCode.RequestEntityTooLarge,
                            "Not found." => (int)HttpStatusCode.NotFound,
                            _ => (int)HttpStatusCode.BadRequest,
                        };
                        break;
                    }

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            _logger.LogInformation($"HTTP Response Information:(" +
                                   $"\"Status Code\":{context.Response.StatusCode} " +
                                   $"\"Schema\":{context.Request.Scheme} " +
                                   $"\"Host\": {context.Request.Host} " +
                                   $"\"Path\": {context.Request.Path} " +
                                   $"\"QueryString\": {context.Request.QueryString} " +
                                   $"\"Content-Length\": {context.Request.ContentLength} " +
                                   $"\"Error\": {exception})");

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(new { error = message }));
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
                BaseException baseException when baseException.Args is not null =>
                    _exceptionsLocalizer.GetString(exceptionName, baseException.Args),
                BaseException =>
                    _exceptionsLocalizer.GetString(exceptionName),
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
