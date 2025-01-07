using Microsoft.AspNetCore.Http.Features;

namespace HEAppE.DataStagingAPI;

/// <summary>
///     Request size check middleware
/// </summary>
public class RequestSizeMiddleware
{
    private readonly RequestDelegate _next;

    public RequestSizeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    ///     Invoke Middleware
    /// </summary>
    /// <param name="context">Context</param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
        var attribute = endpoint?.Metadata.GetMetadata<SizeAttribute>();
        if (attribute != null && context.Request.ContentLength > attribute.Size)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsync("Status Code: 413; Payload Too Large");
        }

        await _next(context);
    }
}