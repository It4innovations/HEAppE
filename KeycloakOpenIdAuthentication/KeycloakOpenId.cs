using System;
using RestSharp;
using HEAppE.KeycloakOpenIdAuthentication.JsonTypes;
using System.Text.RegularExpressions;
using System.Net;
using HEAppE.RestUtils;
using HEAppE.KeycloakOpenIdAuthentication.Exceptions;

namespace HEAppE.KeycloakOpenIdAuthentication
{
    public class KeycloakOpenId
    {
        public KeycloakOpenId()
        {
            // Check client protocol.
            if (KeycloakSettings.Protocol != "openid-connect")
            {
                throw new NotImplementedException("Other client protocol than 'open-id' connect is not implemented. Please switch the client to 'openid-connect'.");
            }
        }

        /// <summary>
        /// Get RestClient for the base keycloak url.
        /// </summary>
        /// <returns>Configured rest client.</returns>
        private RestClient GetBaseClient() => new RestClient(KeycloakSettings.BaseUrl);

        /// <summary>
        /// Authenticate user with password for selected client.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <param name="password">User password.</param>
        /// <param name="clientId">Client name.</param>
        /// <returns></returns>
        private OpenIdUserAuthenticationResult AuthenticateUserWithPasswordImpl(string username, string password, string clientId)
        {
            // NOTE(Moravec): This only supports Public clients, without client_secret.
            RestClient restClient = GetBaseClient();
            IRestRequest restRequest = new RestRequest($"realms/{KeycloakSettings.RealmName}/protocol/{KeycloakSettings.Protocol}/token", Method.POST)
                                       .AddHeader("content-type", "application/x-www-form-urlencoded")
                                       .AddXWwwFormUrlEncodedBody(("client_id", clientId),
                                                                  ("grant_type", "password"),
                                                                  ("username", username),
                                                                  ("password", password));

            var response = restClient.Execute(restRequest);
            return ParseHelper.ParseJsonOrThrow<OpenIdUserAuthenticationResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        public string AuthenticateUserWithPassword(string username, string password)
            => AuthenticateUserWithPasswordImpl(username, password, KeycloakSettings.ClientId).AccessToken;

#if false
        /// <summary>
        /// Retrieve roles for the user.
        /// </summary>
        /// <param name="uuid">Unique user id.</param>
        /// <returns>Collection of roles, assigned to the provided user.</returns>
        public IEnumerable<UserRoleRepresentation> GetUserRoles(string uuid)
        {
            string adminToken = _cachedAdminToken.GetValue();

            var restClient = GetBaseClient();
            var restRequest = new RestRequest($"admin/realms/{KeycloakSettings.RealmName}/users/{uuid}/role-mappings/realm", Method.GET);
            restClient.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(adminToken, "Bearer");

            var response = restClient.Execute(restRequest);
            return ParseHelper.ParseJsonOrThrow<IEnumerable<UserRoleRepresentation>, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// Retrieve groups for the user.
        /// </summary>
        /// <param name="uuid">Unique user id.</param>
        /// <returns>Collection of groups, which is the user member.</returns>
        public IEnumerable<GroupRepresentation> GetUserGroups(string uuid)
        {
            string adminToken = _cachedAdminToken.GetValue();

            var restClient = GetBaseClient();
            var restRequest = new RestRequest($"admin/realms/{KeycloakSettings.RealmName}/users/{uuid}/groups", Method.GET);
            restClient.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(adminToken, "Bearer");

            var response = restClient.Execute(restRequest);
            return ParseHelper.ParseJsonOrThrow<IEnumerable<GroupRepresentation>, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }
#endif

        /// <summary>
        /// Refresh open-id access token with refresh token to obtain new access token.
        /// </summary>
        /// <param name="accessToken">OpenId refresh token.</param>
        /// <returns>Instrospected token info.</returns>
        /// <exception cref="KeycloakOpenIdException">When fails to refresh the token.</exception>
        public OpenIdUserAuthenticationResult RefreshAccessToken(string refreshToken)
        {
            RestClient restClient = new RestClient(KeycloakSettings.BaseUrl);
            RestRequest restRequest = new RestRequest($"realms/{KeycloakSettings.RealmName}/protocol/{KeycloakSettings.Protocol}/token",
                                                      Method.POST);
            restRequest.AddHeader("content-type", "application/x-www-form-urlencoded")
                       .AddXWwwFormUrlEncodedBody(("client_id", KeycloakSettings.ClientId),
                                                  ("grant_type", "refresh_token"),
                                                  ("refresh_token", refreshToken));

            var response = restClient.Execute(restRequest);
            return ParseHelper.ParseJsonOrThrow<OpenIdUserAuthenticationResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// Verify that access tokens match (same iss and sub identifiers...) and extract the refreshed access token.
        /// </summary>
        /// <param name="originalAccessToken">Original access token.</param>
        /// <param name="refreshedAccessToken">Refreshed access token.</param>
        /// <returns>Decoded refreshed access token.</returns>
        /// <exception cref="KeycloakOpenIdException">is thrown when access tokens don't match.</exception>
        /// <exception cref="JwtDecodeException">is thrown when the access token can't be decoded.</exception>
        public DecodedAccessToken VerifyAccessTokensMatchAndExtractToken(string originalAccessToken, string refreshedAccessToken)
        {
            var original = JwtTokenDecoder.Decode(originalAccessToken);
            var refreshed = JwtTokenDecoder.Decode(refreshedAccessToken);

            if (!original.BaseClaimsMatch(refreshed))
            {
                throw new KeycloakOpenIdException("Original OpenId access token can't be matched with the refreshed OpenId access token.");
            }
            return refreshed;
        }

        /// <summary>
        /// Create username for heappe account from openid token info.
        /// </summary>
        /// <param name="decodedAccessToken">Decoded JWT access token.</param>
        /// <returns>Username for the keycloak openid user.</returns>
        public static string CreateOpenIdUsernameForHEAppE(DecodedAccessToken decodedAccessToken)
        {
            const string prefix = "keycloak_";

            // Email is not required when registering user inside keycloak for some weird reason.
            if (decodedAccessToken.IsEmailVerified && !string.IsNullOrWhiteSpace(decodedAccessToken.Email))
            {
                return prefix + decodedAccessToken.Email;
            }
            else
            {
                string usernameWithoutWhitespaces = Regex.Replace(decodedAccessToken.PreferedUsername, @"\s+", " ");
                return prefix + usernameWithoutWhitespaces;
            }
        }
    }
}