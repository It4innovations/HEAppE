using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExternalAuthentication.DTO.JsonTypes;
using HEAppE.RestUtils;
using RestSharp;
using RestSharp.Authenticators;

namespace HEAppE.ExternalAuthentication.KeyCloak;

public sealed class KeycloakOpenId
{
    #region Instances

    /// <summary>
    ///     Get RestClient for the base keycloak url.
    /// </summary>
    /// <returns>Configured rest client.</returns>
    private readonly RestClient _basicRestClient;

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    public KeycloakOpenId()
    {
        if (string.IsNullOrEmpty(ExternalAuthConfiguration.BaseUrl))
            throw new AuthenticationTypeException("OpenId-BadURLForUser");

        if (ExternalAuthConfiguration.Protocol == "openid-connect")
        {
            var options = new RestClientOptions(ExternalAuthConfiguration.BaseUrl)
            {
                Encoding = Encoding.UTF8,
                CachePolicy = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true
                },
                Timeout = TimeSpan.FromMilliseconds(ExternalAuthConfiguration.ConnectionTimeout * 1000)
            };
            _basicRestClient = new RestClient(options);
        }
        else
        {
            throw new AuthenticationTypeException("OpenId-BadProtocol");
        }
    }

    #endregion

    #region Properties

    public async Task<string> AuthenticateUserWithPassword(string username, string password)
    {
        return (await AuthenticateUserWithPasswordImplAsync(username, password, ExternalAuthConfiguration.ClientId))
            .AccessToken;
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Authenticate user with password for selected client.
    ///     <note>This only supports Public clients, without client_secret.</note>
    /// </summary>
    /// <param name="username">User name.</param>
    /// <param name="password">User password.</param>
    /// <param name="clientId">Client name.</param>
    /// <returns></returns>
    /// <exception cref="AuthenticationTypeException">When fails autentication</exception>
    private async Task<OpenIdUserAuthenticationResult> AuthenticateUserWithPasswordImplAsync(string username,
        string password, string clientId)
    {
        var restRequest =
            new RestRequest(
                    $"realms/{ExternalAuthConfiguration.RealmName}/protocol/{ExternalAuthConfiguration.Protocol}/token",
                    Method.Post)
                .AddHeader("content-type", "application/x-www-form-urlencoded")
                .AddParameter("client_id", clientId, ParameterType.GetOrPost)
                .AddParameter("grant_type", "password", ParameterType.GetOrPost)
                .AddParameter("username", username, ParameterType.GetOrPost)
                .AddParameter("password", password, ParameterType.GetOrPost);

        var response = await _basicRestClient.ExecuteAsync(restRequest);
        return ParseHelper.ParseJsonOrThrow<OpenIdUserAuthenticationResult, AuthenticationTypeException>(response,
            HttpStatusCode.OK);
    }

    /// <summary>
    ///     For getting user information.
    /// </summary>
    /// <param name="offlineToken">OpenId offline-token</param>
    /// <returns>Introspected user information</returns>
    /// <exception cref="AuthenticationTypeException">When fails to get user information from token</exception>
    public async Task<KeycloakUserInfoResult> GetUserInfoAsync(string offlineToken)
    {
        var restRequest =
            new RestRequest(
                    $"realms/{ExternalAuthConfiguration.RealmName}/protocol/{ExternalAuthConfiguration.Protocol}/userinfo",
                    Method.Post)
                .AddHeader("content-type", "application/x-www-form-urlencoded")
                .AddParameter("audience", ExternalAuthConfiguration.ClientId, ParameterType.GetOrPost)
                .AddParameter("grant_type", "urn:ietf:params:oauth:grant-type:uma-ticket", ParameterType.GetOrPost)
                .AddParameter("scope", "openid", ParameterType.GetOrPost);
        restRequest.Authenticator = new JwtAuthenticator(offlineToken);

        var response = await _basicRestClient.ExecuteAsync(restRequest);
        return ParseHelper.ParseJsonOrThrow<KeycloakUserInfoResult, AuthenticationTypeException>(response,
            HttpStatusCode.OK);
    }

    /// <summary>
    ///     For exchange access-token to offline-token.
    /// </summary>
    /// <param name="accessToken">OpenId access-token</param>
    /// <returns>Instrospected token information</returns>
    /// <exception cref="AuthenticationTypeException">When fails to exchange token</exception>
    public async Task<OpenIdUserAuthenticationResult> ExchangeTokenAsync(string accessToken)
    {
        var restRequest =
            new RestRequest(
                    $"realms/{ExternalAuthConfiguration.RealmName}/protocol/{ExternalAuthConfiguration.Protocol}/token",
                    Method.Post)
                .AddHeader("content-type", "application/x-www-form-urlencoded")
                .AddParameter("client_secret", ExternalAuthConfiguration.SecretId, ParameterType.GetOrPost)
                .AddParameter("client_id", ExternalAuthConfiguration.ClientId, ParameterType.GetOrPost)
                .AddParameter("scope", "offline_access openid", ParameterType.GetOrPost)
                .AddParameter("requested_token_type", "urn:ietf:params:oauth:token-type:refresh_token",
                    ParameterType.GetOrPost)
                .AddParameter("subject_token_type", "urn:ietf:params:oauth:token-type:access_token",
                    ParameterType.GetOrPost)
                .AddParameter("subject_token", accessToken, ParameterType.GetOrPost)
                .AddParameter("grant_type", "urn:ietf:params:oauth:grant-type:token-exchange", ParameterType.GetOrPost);

        var response = await _basicRestClient.ExecuteAsync(restRequest);
        return ParseHelper.ParseJsonOrThrow<OpenIdUserAuthenticationResult, AuthenticationTypeException>(response,
            HttpStatusCode.OK);
    }

    /// <summary>
    ///     For access-token introspection.
    /// </summary>
    /// <param name="accessToken">OpenId access-token</param>
    /// <returns>Instrospected token info</returns>
    /// <exception cref="AuthenticationTypeException">When fails to introspect token</exception>
    public async Task<KeycloakTokenIntrospectionResult> TokenIntrospectionAsync(string accessToken)
    {
        var restRequest =
            new RestRequest(
                    $"realms/{ExternalAuthConfiguration.RealmName}/protocol/{ExternalAuthConfiguration.Protocol}/token/introspect",
                    Method.Post)
                .AddHeader("content-type", "application/x-www-form-urlencoded")
                .AddParameter("token", accessToken, ParameterType.GetOrPost);
        restRequest.Authenticator =
            new HttpBasicAuthenticator(ExternalAuthConfiguration.ClientId, ExternalAuthConfiguration.SecretId);

        var response = await _basicRestClient.ExecuteAsync(restRequest);
        return ParseHelper.ParseJsonOrThrow<KeycloakTokenIntrospectionResult, AuthenticationTypeException>(response,
            HttpStatusCode.OK);
    }

    /// <summary>
    ///     Refresh open-id access token with refresh token to obtain new access-token.
    /// </summary>
    /// <param name="refreshToken">OpenId refresh-token</param>
    /// <returns>Instrospected token info</returns>
    /// <exception cref="AuthenticationTypeException">When fails to refresh the token</exception>
    public async Task<OpenIdUserAuthenticationResult> RefreshAccessTokenAsync(string refreshToken)
    {
        var restRequest =
            new RestRequest(
                    $"realms/{ExternalAuthConfiguration.RealmName}/protocol/{ExternalAuthConfiguration.Protocol}/token",
                    Method.Post)
                .AddHeader("content-type", "application/x-www-form-urlencoded")
                .AddParameter("refresh_token", refreshToken, ParameterType.GetOrPost)
                .AddParameter("grant_type", "refresh_token", ParameterType.GetOrPost);
        restRequest.Authenticator =
            new HttpBasicAuthenticator(ExternalAuthConfiguration.ClientId, ExternalAuthConfiguration.SecretId);

        var response = await _basicRestClient.ExecuteAsync(restRequest);
        return ParseHelper.ParseJsonOrThrow<OpenIdUserAuthenticationResult, AuthenticationTypeException>(response,
            HttpStatusCode.OK);
    }

    /// <summary>
    ///     Validation open-id introspected token
    /// </summary>
    /// <param name="introspectedToken">Introspected-token</param>
    /// <exception cref="AuthenticationTypeException">When validation is not valid</exception>
    public static void ValidateUserToken(KeycloakTokenIntrospectionResult introspectedToken)
    {
        if (!(introspectedToken.Active &&
              ExternalAuthConfiguration.AllowedClientIds.Contains(introspectedToken.ClientId) &&
              introspectedToken.EmailVerified))
        {
            if (!introspectedToken.Active) throw new AuthenticationTypeException("OpenId-UserNotActive");

            if (!introspectedToken.EmailVerified) throw new AuthenticationTypeException("OpenId-NotVerifiedEmail");

            if (!ExternalAuthConfiguration.AllowedClientIds.Contains(introspectedToken.ClientId))
                throw new AuthenticationTypeException("OpenId-NotInAllowedClientIds");
        }
    }

    #endregion
}