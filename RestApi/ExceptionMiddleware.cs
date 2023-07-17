using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
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
        /// Request delegate
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<ExceptionMiddleware> _logger;

        #endregion
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="next">Request delegate</param>
        /// <param name="logger">Logger</param>
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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
                HandleException(context, ex);
            }
        }
        #endregion
        #region Private Methods
        /// <summary>
        /// Exception handling change Status code
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="exception"></param>
        private void HandleException(HttpContext context, Exception exception)
        {
            switch (exception)
            {
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

        }
        #endregion
    }
}
