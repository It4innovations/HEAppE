using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication;

public class System
{
    #region Properties

    [JsonProperty("all")] public bool All { get; set; }

    #endregion
}