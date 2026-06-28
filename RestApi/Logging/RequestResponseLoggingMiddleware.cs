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
        private readonly bool _isLate;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger, bool isLate = false)
        {
            _next = next;
            _logger = logger;
            _isLate = isLate;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (_isLate)
            {
                context.Items["LateLogging_Executed"] = true;

                // Log request
                _logger.LogInformation($"[Request] Method: {context.Request.Method}, Path: {context.Request.Path}");

                bool isDebugEnabled = _logger.IsEnabled(LogLevel.Debug);
                bool isStreamingEndpoint = context.Request.Path.Value
                    ?.Contains("HttpPostToJobNodeStream", StringComparison.OrdinalIgnoreCase) == true;

                if (!isDebugEnabled || isStreamingEndpoint)
                {
                    await _next(context);
                    return;
                }

                // Request body logging (buffering is already enabled by the early middleware)
                try
                {
                    context.Request.Body.Position = 0;
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

                await _next(context);

                // Log response
                if (context.Response.Body is MemoryStream responseBody)
                {
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
                }
                return;
            }

            // Set requestId in log4net context so it gets logged properly
            log4net.LogicalThreadContext.Properties["requestId"] = context.TraceIdentifier;

            // Add Trace ID/traceparent/Request-ID to Response headers
            if (System.Diagnostics.Activity.Current != null)
            {
                if (!context.Response.Headers.ContainsKey("traceparent"))
                {
                    context.Response.Headers.Append("traceparent", System.Diagnostics.Activity.Current.Id);
                }
                
                var otelTraceId = System.Diagnostics.Activity.Current.TraceId.ToHexString();
                if (!context.Response.Headers.ContainsKey("X-Trace-Id"))
                {
                    context.Response.Headers.Append("X-Trace-Id", otelTraceId);
                }
            }
            else
            {
                if (!context.Response.Headers.ContainsKey("X-Trace-Id"))
                {
                    context.Response.Headers.Append("X-Trace-Id", context.TraceIdentifier);
                }
            }

            if (!context.Response.Headers.ContainsKey("X-Request-Id"))
            {
                context.Response.Headers.Append("X-Request-Id", context.TraceIdentifier);
            }

            bool isDebug = _logger.IsEnabled(LogLevel.Debug);
            bool isStreaming = context.Request.Path.Value
                ?.Contains("HttpPostToJobNodeStream", StringComparison.OrdinalIgnoreCase) == true;

            if (!isDebug || isStreaming)
            {
                try
                {
                    await _next(context);
                }
                finally
                {
                    // Fallback logging for non-debug/streaming early failures
                    if (!context.Items.ContainsKey("LateLogging_Executed"))
                    {
                        _logger.LogInformation($"[Request] Method: {context.Request.Method}, Path: {context.Request.Path}");
                    }
                    log4net.LogicalThreadContext.Properties.Remove("requestId");
                }
                return;
            }

            // Enable buffering early in case we need to read body on fallback
            context.Request.EnableBuffering();

            // Wrap response stream
            var originalBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);
            }
            catch (BadHttpRequestException ex) when (ex.Message.Contains("Unexpected end of request content", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Client disconnected prematurely during request processing.");
                context.Response.Body = originalBodyStream;
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                
                if (!context.Items.ContainsKey("LateLogging_Executed"))
                {
                    _logger.LogInformation($"[Request] Method: {context.Request.Method}, Path: {context.Request.Path}");
                    _logger.LogDebug($"[HEAppE Response] Path: {context.Request.Path}, Status: 400, Body: Client disconnected prematurely.");
                }
                return;
            }
            catch (Exception)
            {
                context.Response.Body = originalBodyStream;
                throw;
            }
            finally
            {
                log4net.LogicalThreadContext.Properties.Remove("requestId");
            }

            // If the late middleware executed, it already logged everything. We just need to copy the buffer back.
            if (context.Items.ContainsKey("LateLogging_Executed"))
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
                return;
            }

            // Fallback logging (under SYSTEM context) for requests that failed before late logging ran
            _logger.LogInformation($"[Request] Method: {context.Request.Method}, Path: {context.Request.Path}");

            // Request body logging
            try
            {
                context.Request.Body.Position = 0;
                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
                {
                    var body = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                    _logger.LogDebug($"[HEAppE Request] Path: {context.Request.Path}, Body: {body}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read request body for logging.");
            }

            // Response body logging
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            try
            {
                using (var reader = new StreamReader(responseBodyStream, leaveOpen: true))
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

            // Copy back the response
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }
}
