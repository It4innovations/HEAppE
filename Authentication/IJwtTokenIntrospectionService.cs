using System.Threading.Tasks;

namespace HEAppE.Authentication;

/// <summary>
/// Service for JWT token introspection
/// </summary>
public interface IJwtTokenIntrospectionService
{
    Task<TokenIntrospectionResult> IntrospectTokenAsync(string token);
}