﻿namespace HEAppE.DomainObjects.UserAndLimitationManagement.Authentication
{
  public class LexisCredentials : AuthenticationCredentials
  {
    /// <summary>
    /// OpenId access token.
    /// </summary>
    public string OpenIdAccessToken { get; set; }

  }
}