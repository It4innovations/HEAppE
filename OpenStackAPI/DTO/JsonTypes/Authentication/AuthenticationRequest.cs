using Newtonsoft.Json;
using System.Collections.Generic;

namespace HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication
{
    public class AuthenticationRequest
    {
        #region Properties
        /// <summary>
        /// Authentication
        /// </summary>
        [JsonProperty("auth")]
        public AuthenticationWrapper Auth { get; set; }
        #endregion
        #region Methods
        /// <summary>
        /// Create unscoped password authentication request object to be json serialized.
        /// </summary>
        /// <param name="serviceAccount">OpenStack service account</param>
        /// <param name="domain">OpenStack domain</param>
        /// <returns>Request object.</returns>
        public static AuthenticationRequest CreateUnscopedAuthenticationPasswordRequest(OpenStackCredentialsDTO serviceAccount, OpenStackDomainDTO domain)
        {
            var request = new AuthenticationRequest
            {
                Auth = new AuthenticationWrapper
                {
                    Identity = new Identity
                    {
                        Methods = new List<string> { "password" },
                        Password = new PasswordAuthentication
                        {
                            User = new User
                            {
                                Id = serviceAccount.Id,
                                Name = serviceAccount.Username,
                                Password = serviceAccount.Password,
                                Domain = new Domain
                                {
                                    Name = domain.Name
                                }
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
        /// <param name="project">OpenStack project</param>
        /// <param name="scope">Scope to be authorized for</param>
        /// <returns>Request object.</returns>
        public static AuthenticationRequest CreateScopedAuthenticationPasswordRequest(OpenStackProjectDTO project, Scope scope)
        {
            var req = CreateUnscopedAuthenticationPasswordRequest(project.Credentials, project.Domain);
            req.Auth.Scope = scope;
            return req;
        }

        /// <summary>
        /// Create scoped password authentication request object to be json serialized.
        /// Scoped authentication returns token valid in selected scope.
        /// </summary>
        /// <param name="project">OpenStack project</param>
        /// <returns>Request object.</returns>
        public static AuthenticationRequest CreateScopedAuthenticationPasswordRequest(OpenStackProjectDTO project)
        {
            var projectDomain = project.Domain;
            var req = CreateUnscopedAuthenticationPasswordRequest(project.Credentials, projectDomain);

            var domain = projectDomain is not null
                ? new Domain()
                {
                    Id = projectDomain.UID,
                    Name = projectDomain.Name
                }
                : null;

            req.Auth.Scope = new Scope
            {
                Project = new Project
                {
                    Id = project.UID,
                    Name = project.Name,
                    Domain = domain ?? default
                }
            };
            return req;
        }
        #endregion
    }
}