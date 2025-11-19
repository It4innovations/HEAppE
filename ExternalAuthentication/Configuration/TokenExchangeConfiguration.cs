namespace HEAppE.ExternalAuthentication.Configuration;

public class TokenExchangeConfiguration
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string GrantType { get; set; } = "urn:ietf:params:oauth:grant-type:token-exchange";
    public string SubjectTokenType { get; set; } = "urn:ietf:params:oauth:token-type:access_token";
    public string Audience { get; set; }

}