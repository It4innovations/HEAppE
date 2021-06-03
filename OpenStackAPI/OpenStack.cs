using System;
using System.Linq;
using System.Net;
using HEAppE.MiddlewareUtils.Caching;
using HEAppE.OpenStackAPI.DTO;
using HEAppE.OpenStackAPI.Exceptions;
using HEAppE.OpenStackAPI.JsonTypes.Authentication;
using HEAppE.RestUtils;
using Newtonsoft.Json;
using RestSharp;

namespace HEAppE.OpenStackAPI
{
    public class OpenStack
    {
        public class OpenStackInfo
        {
            public string Domain { get; set; }
            public string OpenStackUrl { get; set; }
            public string ServiceAccUsername { get; set; }
            public string ServiceAccPassword { get; set; }
        }

        private readonly OpenStackInfo _osInfo;

        /// <summary>
        /// Cached service account token.
        /// </summary>
        private readonly Cached<AuthenticationResponse> _cachedServiceAcc;

        /// <summary>
        /// Create OpenStack API client.
        /// </summary>
        /// <param name="osInfo">Information about the open stack instance..</param>
        public OpenStack(OpenStackInfo osInfo)
        {
            _osInfo = osInfo;
            // NOTE(Moravec): Service account token loading callback.
            _cachedServiceAcc = new Cached<AuthenticationResponse>(() =>
            {
                try
                {
                    var authenticationResult = AuthenticateUnscoped(_osInfo.ServiceAccUsername, _osInfo.ServiceAccPassword, _osInfo.Domain);

                    int expirationTime = int.MaxValue;
                    if (authenticationResult.Token.ExpiresAt.HasValue)
                    {
                        long seconds = (long) (authenticationResult.Token.ExpiresAt.Value - DateTime.UtcNow).TotalSeconds;
                        if (seconds <= int.MaxValue)
                        {
                            expirationTime = (int) seconds;
                        }
                    }

                    return new Cached<AuthenticationResponse>.CacheLoadResult(authenticationResult, expirationTime);
                }
                catch (OpenStackAPIException ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    Console.Error.WriteLine(ex);
                    throw;
                }
            }, false);
        }

        private string CombineUrlWithPort(string url, int port) => $"{url}:{port}/";

        private string CreateIdentityEndpointUrl() => CombineUrlWithPort(_osInfo.OpenStackUrl, OpenStackSettings.IdentityPort);

        /// <summary>
        /// Authenticate the user with password for no specific scopes.
        /// </summary>
        /// <param name="userName">User name.</param>
        /// <param name="password">User password.</param>
        /// <param name="domain">Domain for authentication.</param>
        /// <returns>Authentication response from the rest api with the authentication token.</returns>
        /// <exception cref="OpenStackAPIException">Is thrown when the request is malformed and the API returns non 201 code.</exception>
        private AuthenticationResponse AuthenticateUnscoped(string userName, string password, string domain)
        {
            var requestObject = AuthenticationRequest.CreateUnscopedAuthenticationPasswordRequest(userName, password, domain);
            string requestBody = JsonConvert.SerializeObject(requestObject, IgnoreNullSerializer.Instance);

            RestClient rest = new RestClient(CreateIdentityEndpointUrl());
            IRestRequest request = new RestRequest($"v{OpenStackSettings.OpenStackVersion}/auth/tokens", Method.POST)
                .AddSerializedJsonBody(requestBody);

            IRestResponse response = rest.Execute(request);
            AuthenticationResponse result =
                ParseHelper.ParseJsonOrThrow<AuthenticationResponse, OpenStackAPIException>(response, HttpStatusCode.Created);

            result.AuthToken = (string) response.Headers.Single(p => p.Name == "X-Subject-Token").Value;
            return result;
        }

        /// <summary>
        /// Create application credentials bound to service account with given unique name and expiration time.
        /// </summary>
        /// <param name="requestedUserName">Username of the requester.</param>
        /// <returns>Created application credentials.</returns>
        public ApplicationCredentialsDTO CreateApplicationCredentials(string requestedUserName)
        {
            string uniqueTokenName = requestedUserName + '_' + Guid.NewGuid();

            var serviceAcc = _cachedServiceAcc.GetValue();
            var sessionExpiresAt = DateTime.UtcNow.AddSeconds(OpenStackSettings.OpenStackSessionExpiration);

            var requestObject = ApplicationCredentialsRequest.CreateApplicationCredentialsRequest(uniqueTokenName, sessionExpiresAt);
            string requestBody = JsonConvert.SerializeObject(requestObject, IgnoreNullSerializer.Instance);

            var rest = new RestClient(CreateIdentityEndpointUrl());

            string requestUrl = $"v{OpenStackSettings.OpenStackVersion}/users/{serviceAcc.Token.User.Id}/application_credentials";
            var request = new RestRequest(requestUrl, Method.POST)
                          .AddSerializedJsonBody(requestBody)
                          .AddXAuthTokenToHeader(serviceAcc.AuthToken);

            IRestResponse response = rest.Execute(request);
            ApplicationCredentialsResponse result =
                ParseHelper.ParseJsonOrThrow<ApplicationCredentialsResponse, OpenStackAPIException>(response, HttpStatusCode.Created);


            return new ApplicationCredentialsDTO
            {
                ApplicationCredentialsId = result.ApplicationCredentials.Id,
                ApplicationCredentialsSecret = result.ApplicationCredentials.Secret,
                ExpiresAt = sessionExpiresAt
            };
        }
    }
}