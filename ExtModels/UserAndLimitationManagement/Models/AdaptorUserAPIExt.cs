using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// Adaptor user ext
/// </summary>
[DataContract(Name = "AdaptorUserCreatedExt")]
[Description("Adaptor user Created ext")]
public class AdaptorUserCreatedExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long? Id { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    [DataMember(Name = "Username")]
    [Description("Username")]
    public string Username { get; set; }
    
    /// <summary>
    /// API key
    /// </summary>
    /// <returns></returns>
    [DataMember(Name = "ApiKey")]
    [Description("API key")]
    public string ApiKey { get; set; }
    
    
    public override string ToString()
    {
        return $"AdaptorUserAPIExt(id={Id}; username={Username})";
    }
}