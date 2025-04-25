using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication;

public class AuthenticationWrapper
{
    #region Properties

    [JsonProperty("identity")] public Identity Identity { get; set; }

    [JsonProperty("scope")] public Scope Scope { get; set; }

    #endregion
}