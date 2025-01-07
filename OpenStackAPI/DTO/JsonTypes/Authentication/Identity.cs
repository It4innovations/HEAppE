using System.Collections.Generic;
using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication;

public class Identity
{
    #region Properties

    [JsonProperty("methods")] public List<string> Methods { get; set; }

    [JsonProperty("password")] public PasswordAuthentication Password { get; set; }

    [JsonProperty("scope")] public Project Scope { get; set; }

    #endregion
}