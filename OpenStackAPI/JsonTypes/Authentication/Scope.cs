using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class Scope
    {
        #region Properties
        [JsonProperty("system")]
        public System System { get; set; }

        [JsonProperty("domain")]
        public Domain Domain { get; set; }

        [JsonProperty("project")]
        public Project Project { get; set; }
        #endregion
    }
}