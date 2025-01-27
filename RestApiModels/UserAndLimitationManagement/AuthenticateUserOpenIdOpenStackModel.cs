using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.UserAndLimitationManagement;

/// <summary>
/// Model to authenticate with OpenId open stack
/// </summary>
[DataContract(Name = "AuthenticateUserOpenIdOpenStackModel")]
[Description("Model to authenticate with OpenId open stack")]
public class AuthenticateUserOpenIdOpenStackModel : AuthenticateUserOpenIdModel
{
    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Project id")]
    public long ProjectId { get; set; }

    public override string ToString()
    {
        return $"AuthenticateUserOpenIdOpenStackModel({base.ToString()}; ProjectId: {ProjectId})";
    }
}