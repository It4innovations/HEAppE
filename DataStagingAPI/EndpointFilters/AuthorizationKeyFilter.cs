using HEAppE.DataStagingAPI.Configuration;
using Microsoft.Extensions.Options;

namespace HEAppE.DataStagingAPI
{
    /// <summary>
    /// Authorization filter
    /// </summary>
    public class AuthorizationKeyFilter : IEndpointFilter
    {
        /// <summary>
        /// options
        /// </summary>
        private readonly ApplicationAPIOptions _options;

        public AuthorizationKeyFilter(IOptions<ApplicationAPIOptions> options)
        {
            _options = options.Value;
        }

        /// <summary>
        /// Apply filter
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="next">Next</param>
        /// <returns></returns>
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
