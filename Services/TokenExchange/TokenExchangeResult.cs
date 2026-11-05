namespace HEAppE.Services.TokenExchange;

/// <summary>
/// Result of a token exchange operation.
/// </summary>
public class TokenExchangeResult
{
    /// <summary>
    /// Whether the exchange was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The exchanged access token.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Optional refresh token (if requested).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token expiration in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// The issued token type (e.g., urn:ietf:params:oauth:token-type:access_token).
    /// </summary>
    public string? IssuedTokenType { get; set; }

    /// <summary>
    /// Error message if the exchange failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Name of the target this result belongs to.
    /// </summary>
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static TokenExchangeResult Failed(string targetName, string error) => new()
    {
        Success = false,
        TargetName = targetName,
        Error = error
    };

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static TokenExchangeResult Succeeded(string targetName, string accessToken, int expiresIn = 0) => new()
    {
        Success = true,
        TargetName = targetName,
        AccessToken = accessToken,
        ExpiresIn = expiresIn
    };
}
