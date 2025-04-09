namespace HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;

public class OpenIdCredentials : AuthenticationCredentials
{
  /// <summary>
  ///     OpenId access token.
  /// </summary>
  public string OpenIdAccessToken { get; set; }
}