namespace HEAppE.Authentication;

/// <summary>
/// Token introspection result
/// </summary>
public class TokenIntrospectionResult
{
    public bool Active { get; set; }
    public string? Sub { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public long? Exp { get; set; }
    public long? Iat { get; set; }
    public string? Scope { get; set; }
    public string? ClientId { get; set; }
    public string? TokenType { get; set; }
        
    /// <summary>
    /// Check if token is active and not expired
    /// </summary>
    public bool IsValid => Active && (Exp == null || DateTimeOffset.FromUnixTimeSeconds(Exp.Value) > DateTimeOffset.UtcNow);

    public override string ToString()
    {
        return $"Active: {Active}, Sub: {Sub}, Username: {Username}, Email: {Email}, Name: {Name}, Exp: {Exp}, Iat: {Iat}, Scope: {Scope}, ClientId: {ClientId}, TokenType: {TokenType}";
    }
}