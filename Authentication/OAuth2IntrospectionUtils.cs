using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace HEAppE.Authentication;

public class OAuth2IntrospectionUtils
{
    // <summary>
    /// Extract relevant claims from OAuth2 introspection result
    /// </summary>
    /// <returns>Dictionary of claims from introspection</returns>
    public static Dictionary<string, object> ExtractIntrospectionClaims(ClaimsPrincipal user, ILogger logger)
    {
        var claims = new Dictionary<string, object>();
    
        if (user?.Identity?.IsAuthenticated == true)
        {
            // Extract standard OAuth2 introspection claims
            claims["sub"] = user.FindFirst("sub")?.Value; // Subject (user ID)
            claims["client_id"] = user.FindFirst("client_id")?.Value; // Client identifier
            claims["username"] = user.FindFirst("username")?.Value; // Username
            claims["scope"] = user.FindFirst("scope")?.Value; // Authorized scopes
            claims["exp"] = user.FindFirst("exp")?.Value; // Expiration time
            claims["iat"] = user.FindFirst("iat")?.Value; // Issued at time
            claims["active"] = user.FindFirst("active")?.Value; // Token active status
        
            // Extract custom claims (adjust based on your authorization server setup)
            claims["roles"] = user.FindAll("role").Select(c => c.Value).ToArray();
            claims["permissions"] = user.FindAll("permission").Select(c => c.Value).ToArray();
        
            // Log extracted claims for debugging
            logger.LogDebug($"Extracted introspection claims: {string.Join(", ", claims.Where(kv => kv.Value != null).Select(kv => $"{kv.Key}: {kv.Value}"))}");
        }
        else
        {
            logger.LogWarning("User is not authenticated or introspection claims are missing");
        }
    
        return claims;
    }
}