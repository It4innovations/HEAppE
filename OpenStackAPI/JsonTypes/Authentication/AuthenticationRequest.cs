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
        /// <returns>Request object.</returns>
        public static AuthenticationRequest CreateUnscopedAuthenticationPasswordRequest(OpenStackServiceAccDTO serviceAccount) 
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
                                //Domain = serviceAccount.Domain is null ? null: new Domain 
                                //{ 
                                //    Id = serviceAccount.Domain.Id,
                                //    Name = serviceAccount.Domain.Name
                                //}
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
        /// <param name="scope">Scope to be authorized for.</param>
        /// <returns>Request object.</returns>
        public static AuthenticationRequest CreateScopedAuthenticationPasswordRequest(OpenStackServiceAccDTO serviceAccount, Scope scope)
        {
            AuthenticationRequest req = CreateUnscopedAuthenticationPasswordRequest(serviceAccount);
            req.Auth.Scope = scope;
            return req;
        }

        /// <summary>
        /// Create scoped password authentication request object to be json serialized.
        /// Scoped authentication returns token valid in selected scope.
        /// </summary>
        /// <param name="serviceAccount">OpenStack service account.</param>
        /// <param name="projects">OpenStack projects</param>
        /// <returns>Request object.</returns>
        public static AuthenticationRequest CreateScopedAuthenticationPasswordRequest(OpenStackServiceAccDTO serviceAccount, IEnumerable<OpenStackProjectDTO> projects)
        {
            AuthenticationRequest req = CreateUnscopedAuthenticationPasswordRequest(serviceAccount);
            var OpenStackproject = projects.FirstOrDefault();
            req.Auth.Scope = new Scope
            {
                Project = new Project
                {
                    Id = OpenStackproject.Id,
                    Name = OpenStackproject.Name,
                    Domain = new Domain 
                    {
                        Id = OpenStackproject.ProjectDomains.FirstOrDefault()?.Id,
                        Name = OpenStackproject.ProjectDomains.FirstOrDefault()?.Name
                    }
                }
            };
            return req;
        }
        #endregion
    }
}