using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication;

public class Role
{
    #region Properties

    [JsonProperty("role")] public string Name { get; set; }

    #endregion
}