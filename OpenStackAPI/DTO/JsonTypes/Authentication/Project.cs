using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication;

public class Project : Identifiable
{
    #region Properties

    [JsonProperty("domain")] public Domain Domain { get; set; }

    #endregion
}