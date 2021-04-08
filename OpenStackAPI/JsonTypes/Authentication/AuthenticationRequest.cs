using System.Collections.Generic;
using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class AuthenticationRequest
    {
        public class AuthenticationWrapper
        {
            [JsonProperty("identity")]
            public Identity Identity { get; set; }

            [JsonProperty("scope")]
            public Scope Scope { get; set; }
        }

        [JsonProperty("auth")]
        public AuthenticationWrapper Auth { get; set; }

        /// <summary>
        /// Create unscoped password authentication request object to be json serialized.
        /// </summary>
        /// <param name="userName">User name.</param>
        /// <param name="password">User password.</param>
        /// <param name="domain">Domain to be authorized for.</param>
        /// <returns>Request object.</returns>
        public static AuthenticationRequest CreateUnscopedAuthenticationPasswordRequest(string userName, string password, string domain)
        {
            var request = new AuthenticationRequest
            {
                Auth = new AuthenticationWrapper
                {
                    Identity = new Identity
                    {
                        Methods = new List<string> {"password"},
                        Password = new PasswordAuthentication
                        {
                            User = new User
                            {
                                Name = userName,
                                Password = password,
                                Domain = new Domain {Name = domain}
                            }
                        }
                    }
                }
            };

            return request;
        }

        /// <summary>
        /// Create scoped password authentication request object to be json serialized.
        /// Scoped authentication returns token valid in selected scope.
        /// </summary>
        /// <param name="userName">User name.</param>
        /// <param name="password">User password.</param>
        /// <param name="domain">Domain to be authorized for.</param>
        /// <param name="scope">Scope to be authorized for.</param>
        /// <returns>Request object.</returns>
        public static AuthenticationRequest CreateScopedAuthenticationPasswordRequest(string userName,
                                                                                      string password,
                                                                                      string domain,
                                                                                      Scope scope)
        {
            AuthenticationRequest req = CreateUnscopedAuthenticationPasswordRequest(userName, password, domain);
            req.Auth.Scope = scope;
            return req;
        }
    }
}