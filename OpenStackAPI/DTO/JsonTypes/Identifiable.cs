using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.DTO.JsonTypes
{
    public abstract class Identifiable
    {
        #region Properties
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
        #endregion
    }
}