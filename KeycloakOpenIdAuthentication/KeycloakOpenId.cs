using System;
using RestSharp;
using HEAppE.KeycloakOpenIdAuthentication.JsonTypes;
using System.Net;
using HEAppE.RestUtils;
using HEAppE.KeycloakOpenIdAuthentication.Exceptions;
using RestSharp.Authenticators;
using System.Text;
using System.Net.Cache;
using HEAppE.KeycloakOpenIdAuthentication.Configuration;
using System.Linq;

namespace HEAppE.KeycloakOpenIdAuthentication
{
    public sealed class KeycloakOpenId
    {
        #region Instances
        /// <summary>
        /// Get RestClient for the base keycloak url.
        /// </summary>
        /// <returns>Configured rest client.</returns>
        private readonly IRestClient _basicRestClient;
        #endregion
        #region Properties
        public string AuthenticateUserWithPassword(string username, string password) => AuthenticateUserWithPasswordImpl(username, password, KeycloakConfiguration.ClientId).AccessToken;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public KeycloakOpenId()
        {
            if (KeycloakConfiguration.Protocol == "openid-connect")
            {
                _basicRestClient = new RestClient(KeycloakConfiguration.BaseUrl)
                {
                    Encoding = Encoding.UTF8,
                    CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)
                };
            }
            else
            {
                throw new NotImplementedException("Other client protocol than 'open-id' connect is not implemented. Please switch the client to 'openid-connect'.");
            }

        }
        #endregion
        #region Methods
        /// <summary>
        /// Authenticate user with password for selected client.
        /// <note>This only supports Public clients, without client_secret.</note>
        /// </summary>
        /// <param name="username">User name.</param>
        /// <param name="password">User password.</param>
        /// <param name="clientId">Client name.</param>
        /// <returns></returns>
        /// <exception cref="KeycloakOpenIdException">When fails autentication</exception>
        private OpenIdUserAuthenticationResult AuthenticateUserWithPasswordImpl(string username, string password, string clientId)
        {
            IRestRequest restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/token", Method.POST)
                                       .AddHeader("content-type", "application/x-www-form-urlencoded")
                                        .AddXWwwFormUrlEncodedBody(("client_id", clientId),
                                                                   ("grant_type", "password"),
                                                                   ("username", username),
                                                                   ("password", password));

            var response = _basicRestClient.Execute(restRequest);
            return ParseHelper.ParseJsonOrThrow<OpenIdUserAuthenticationResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// For getting user information.
        /// </summary>
        /// <param name="offlineToken">OpenId offline-token</param>
        /// <returns>Introspected user information</returns>
        /// <exception cref="KeycloakOpenIdException">When fails to get user information from token</exception>
        public KeycloakUserInfoResult GetUserInfo(string offlineToken)
        {
            _basicRestClient.Authenticator = new JwtAuthenticator(offlineToken);
            IRestRequest restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/userinfo", Method.POST)
                                        .AddHeader("content-type", "application/x-www-form-urlencoded")
                                         .AddXWwwFormUrlEncodedBody(("audience", KeycloakConfiguration.ClientId),
                                                                    ("grant_type", "urn:ietf:params:oauth:grant-type:uma-ticket"));

            var response = _basicRestClient.Execute(restRequest);
            return ParseHelper.ParseJsonOrThrow<KeycloakUserInfoResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// For exchange access-token to offline-token.
        /// </summary>
        /// <param name="accessToken">OpenId access-token</param>
        /// <returns>Instrospected token information</returns>
        /// <exception cref="KeycloakOpenIdException">When fails to exchange token</exception>
        public OpenIdUserAuthenticationResult ExchangeToken(string accessToken)
        {
            IRestRequest restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/token", Method.POST)
                                        .AddHeader("content-type", "application/x-www-form-urlencoded")
                                         .AddXWwwFormUrlEncodedBody(("client_secret", KeycloakConfiguration.SecretId),
                                                                    ("client_id", KeycloakConfiguration.ClientId),
                                                                    ("scope", "offline_access"),
                                                                    ("requested_token_type", "urn:ietf:params:oauth:token-type:refresh_token"),
                                                                    ("subject_token_type", "urn:ietf:params:oauth:token-type:access_token"),
                                                                    ("subject_token", accessToken),
                                                                    ("grant_type", "urn:ietf:params:oauth:grant-type:token-exchange"));

            var response = _basicRestClient.Execute(restRequest);
            return ParseHelper.ParseJsonOrThrow<OpenIdUserAuthenticationResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// For access-token introspection.
        /// </summary>
        /// <param name="accessToken">OpenId access-token</param>
        /// <returns>Instrospected token info</returns>
        /// <exception cref="KeycloakOpenIdException">When fails to introspect token</exception>
        public KeycloakTokenIntrospectionResult TokenIntrospection(string accessToken)
        {
            _basicRestClient.Authenticator = new HttpBasicAuthenticator(KeycloakConfiguration.ClientId, KeycloakConfiguration.SecretId);
            IRestRequest restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/token/introspect", Method.POST)
                                        .AddHeader("content-type", "application/x-www-form-urlencoded")
                                         .AddXWwwFormUrlEncodedBody(("token", accessToken));

            var response = _basicRestClient.Execute(restRequest);
            return ParseHelper.ParseJsonOrThrow<KeycloakTokenIntrospectionResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// Refresh open-id access token with refresh token to obtain new access-token.
        /// </summary>
        /// <param name="refreshToken">OpenId refresh-token</param>
        /// <returns>Instrospected token info</returns>
        /// <exception cref="KeycloakOpenIdException">When fails to refresh the token</exception>
        public OpenIdUserAuthenticationResult RefreshAccessToken(string refreshToken)
        {
            _basicRestClient.Authenticator = new HttpBasicAuthenticator(KeycloakConfiguration.ClientId, KeycloakConfiguration.SecretId);
            IRestRequest restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/token", Method.POST)
                                        .AddHeader("content-type", "application/x-www-form-urlencoded")
                                         .AddXWwwFormUrlEncodedBody(("refresh_token", refreshToken),
                                                                    ("grant_type", "refresh_token"));

            var response = _basicRestClient.Execute(restRequest);
            return ParseHelper.ParseJsonOrThrow<OpenIdUserAuthenticationResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// Validation open-id introspected token
        /// </summary>
        /// <param name="introspectedToken">Introspected-token</param>
        /// <exception cref="KeycloakOpenIdException">When validation is not valid</exception>
        public static void ValidateUserToken(KeycloakTokenIntrospectionResult introspectedToken)
        {
            if (! (introspectedToken.Active && KeycloakConfiguration.AllowedClientIds.Contains(introspectedToken.ClientId) && introspectedToken.EmailVerified))
            {
                StringBuilder textBuilder = new ();
                if (!introspectedToken.Active)
                {
                    textBuilder.AppendLine("Open-Id: User is not active!");
                }

                if (!introspectedToken.EmailVerified)
                {
                    textBuilder.AppendLine("Open-Id: User does not verified email!");
                }

                if (!KeycloakConfiguration.AllowedClientIds.Contains(introspectedToken.ClientId))
                {
                    textBuilder.AppendLine("Open-Id: User is not in allowed clientIds!");
                }
                
                throw new KeycloakOpenIdException(textBuilder.ToString());
            }
        }
        #endregion
    }
}