using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class ApplicationCredentials : Identifiable
    {
        [JsonProperty("secret")]
        public string Secret { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [JsonProperty("roles")]
        public List<Role> Roles { get; set; }

        [JsonProperty("access_rules")]
        public List<AccessRule> AccessRules { get; set; }

        [JsonProperty("unrestricted")]
        public bool Unrestricted { get; set; }

        [JsonProperty("links")]
        public Dictionary<string, string> Links { get; set; }

        [JsonProperty("project_id")]
        public string ProjectId { get; set; }
    }

    public class ApplicationCredentialsRequest
    {
        [JsonProperty("application_credential")]
        public ApplicationCredentials ApplicationCredentials { get; set; }

        public static ApplicationCredentialsRequest CreateApplicationCredentialsRequest(string uniqueName,
                                                                                        DateTime expiration,
                                                                                        bool restricted = true,
                                                                                        IEnumerable<Role> roles = null,
                                                                                        IEnumerable<AccessRule> accessRules = null)
        {
            var request = new ApplicationCredentialsRequest
            {
                ApplicationCredentials = new ApplicationCredentials
                {
                    Name = uniqueName,
                    ExpiresAt = expiration,
                    Unrestricted = !restricted,
                    Description = "Application credentials created by OpenStackAPI library of HEAppE."
                }
            };
            if (roles != null)
                request.ApplicationCredentials.Roles = roles.ToList();
            if (accessRules != null)
                request.ApplicationCredentials.AccessRules = accessRules.ToList();
            return request;
        }
    }
}