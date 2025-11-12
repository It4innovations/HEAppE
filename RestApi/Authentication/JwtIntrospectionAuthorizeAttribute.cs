using System;
using HEAppE.Authentication;
using Microsoft.AspNetCore.Http;

namespace HEAppE.RestApi.Authentication;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

public class JwtIntrospectionAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        var introspectionService = context.HttpContext.RequestServices.GetService<IJwtTokenIntrospectionService>();

        if (introspectionService == null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }

        var result = await introspectionService.IntrospectTokenAsync(token);

        if (result == null || !result.IsValid)
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        context.HttpContext.Items["User"] = result;
    }
}
