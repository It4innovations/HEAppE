using Newtonsoft.Json;

namespace HEAppE.ExternalAuthentication.DTO.JsonTypes;

public class AccessRightsResult
{
    /// <summary>
    ///     Proect Id
    /// </summary>
    [JsonProperty("PRJ_UUID")]
    public string ProjectId { get; set; }

    /// <summary>
    ///     Project name.
    /// </summary>
    [JsonProperty("PRJ")]
    public string ProjectName { get; set; }
}