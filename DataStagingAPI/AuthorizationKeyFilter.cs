using HEAppE.DataStagingAPI.Configuration;
using Microsoft.Extensions.Options;

namespace HEAppE.DataStagingAPI
{
    public class AuthorizationKeyFilter : IEndpointFilter
    {
        private readonly ApplicationAPIOptions _options;

        public AuthorizationKeyFilter(IOptions<ApplicationAPIOptions> options)
        {
            _options = options.Value;
        }


        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            if (!(context.HttpContext.Request.Headers.TryGetValue(_options.AuthenticationParamHeaderName, out var authKey) && authKey == _options.AuthenticationToken))
            {
                return TypedResults.Unauthorized();
            }
            
            return await next(context);
        }
    }
}
