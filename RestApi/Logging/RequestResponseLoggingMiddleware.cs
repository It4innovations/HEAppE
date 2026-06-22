using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HEAppE.RestApi.Logging
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Set requestId in log4net context so it gets logged properly
            log4net.LogicalThreadContext.Properties["requestId"] = context.TraceIdentifier;

            try
            {
                _logger.LogInformation($"[Request] Method: {context.Request.Method}, Path: {context.Request.Path}");

                bool isDebugEnabled = _logger.IsEnabled(LogLevel.Debug);
                bool isStreamingEndpoint = context.Request.Path.Value
                    ?.Contains("HttpPostToJobNodeStream", StringComparison.OrdinalIgnoreCase) == true;

                if (!isDebugEnabled || isStreamingEndpoint)
                {
                    await _next(context);
                    return;
                }

                // Request logging
                context.Request.EnableBuffering();
                try
                {
                    using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
                    {
                        var body = await reader.ReadToEndAsync();
                        context.Request.Body.Position = 0;
                        _logger.LogDebug($"[HEAppE Request] Path: {context.Request.Path}, Body: {body}");
                    }
                }
                catch (BadHttpRequestException ex) when (ex.Message.Contains("Unexpected end of request content", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Client disconnected prematurely while sending request body.");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read request body for logging.");
                }

                // Response logging via buffering
                var originalBodyStream = context.Response.Body;
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                try
                {
                    await _next(context);
                }
                catch (BadHttpRequestException ex) when (ex.Message.Contains("Unexpected end of request content", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Client disconnected prematurely during request processing.");
                    context.Response.Body = originalBodyStream;
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
                catch (Exception)
                {
                    context.Response.Body = originalBodyStream;
                    throw;
                }

                // Log response
                responseBody.Seek(0, SeekOrigin.Begin);
                try
                {
                    using (var reader = new StreamReader(responseBody, leaveOpen: true))
                    {
                        var responseText = await reader.ReadToEndAsync();
                        _logger.LogDebug(
                            $"[HEAppE Response] Path: {context.Request.Path}, Status: {context.Response.StatusCode}, Body: {responseText}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read response body log context, connection might be aborted.");
                }

                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
            }
            finally
            {
                log4net.LogicalThreadContext.Properties.Remove("requestId");
            }
        }
    }
}
