using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication;

public class Token
{
    #region Properties

    [JsonProperty("methods")] public List<string> Methods { get; set; }

    [JsonProperty("user")] public User User { get; set; }

    [JsonProperty("audit_ids")] public List<string> AuditIds { get; set; }

    [JsonProperty("expires_at")] public DateTime? ExpiresAt { get; set; }

    [JsonProperty("issued_at")] public DateTime IssuedAt { get; set; }

    #endregion
}