using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class ApplicationCredentials : Identifiable
    {
        #region Properties
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
        #endregion
    }
}