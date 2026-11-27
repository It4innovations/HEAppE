using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HEAppE.RestApi.Authentication;

/// <summary>
/// Restricts endpoint access to requests originating from localhost (loopback).
/// Also rejects requests forwarded from non-loopback client IPs via proxy headers.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class LocalhostOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;
        var remoteIp = httpContext.Connection.RemoteIpAddress;

        if (!IsLoopbackAddress(remoteIp) || HasNonLoopbackForwardedHeader(httpContext.Request.Headers))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
    }

    private static bool IsLoopbackAddress(IPAddress? address)
    {
        if (address == null) return false;
        if (IPAddress.IsLoopback(address)) return true;
        return address.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(address.MapToIPv4());
    }

    private static bool HasNonLoopbackForwardedHeader(IHeaderDictionary headers)
    {
        if (headers.TryGetValue("X-Real-Ip", out var realIpValues))
        {
            foreach (var raw in realIpValues)
            {
                if (!string.IsNullOrWhiteSpace(raw) &&
                    (!IPAddress.TryParse(raw.Trim(), out var ip) || !IsLoopbackAddress(ip)))
                {
                    return true;
                }
            }
        }

        if (headers.TryGetValue("X-Forwarded-For", out var forwardedValues))
        {
            foreach (var raw in forwardedValues)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!IPAddress.TryParse(part, out var ip) || !IsLoopbackAddress(ip))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
