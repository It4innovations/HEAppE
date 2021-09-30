using System.Collections.Generic;
using System.Linq;
using HEAppE.OpenStackAPI.DTO;
using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
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
        /// <param name="serviceAccount">OpenStack service account.</param>
        /// <param name="openStackDomain">OpenStack domain name.</param>
        /// <returns>Request object.</returns>
        public static AuthenticationRequest CreateUnscopedAuthenticationPasswordRequest(OpenStackServiceAccDTO serviceAccount, string openStackDomain) 
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
                                Domain =  new Domain 
                                { 
                                    Name = openStackDomain
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
        /// <param name="serviceAccount">OpenStack service account.</param>
        /// <param name="project">OpenStack project</param>
        /// <param name="scope">Scope to be authorized for.</param>
        /// <returns>Request object.</returns>
        public static AuthenticationRequest CreateScopedAuthenticationPasswordRequest(OpenStackServiceAccDTO serviceAccount, OpenStackProjectDTO project, Scope scope)
        {
            AuthenticationRequest req = CreateUnscopedAuthenticationPasswordRequest(serviceAccount, project.Domain.Name);
            req.Auth.Scope = scope;
            return req;
        }

        /// <summary>
        /// Create scoped password authentication request object to be json serialized.
        /// Scoped authentication returns token valid in selected scope.
        /// </summary>
        /// <param name="serviceAccount">OpenStack service account.</param>
        /// <param name="project">OpenStack project</param>
        /// <returns>Request object.</returns>
        public static AuthenticationRequest CreateScopedAuthenticationPasswordRequest(OpenStackServiceAccDTO serviceAccount, OpenStackProjectDTO project)
        {
            AuthenticationRequest req = CreateUnscopedAuthenticationPasswordRequest(serviceAccount, project.Domain.Name);

            var projectDomain = project.ProjectDomains.FirstOrDefault();
            var domain = projectDomain is not null
                ? new Domain()
                    {
                        Id = projectDomain.Id,
                        Name = projectDomain.Name
                    }
                : null;

            req.Auth.Scope = new Scope
            {
                Project = new Project
                {
                    Id = project.Id,
                    Name = project.Name,
                    Domain = domain ?? default
                }
            };
            return req;
        }
        #endregion
    }
}