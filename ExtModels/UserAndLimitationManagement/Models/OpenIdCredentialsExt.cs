using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// OpenId credentials ext
/// </summary>
[DataContract(Name = "OpenIdCredentialsExt")]
[Description("OpenId credentials ext")]
public class OpenIdCredentialsExt : AuthenticationCredentialsExt
{
    /// <summary>
    /// OpenId access token
    /// </summary>
    [Required]
    [DataMember(Name = nameof(OpenIdAccessToken))]
    [Description("OpenId access token")]
    public string OpenIdAccessToken { get; set; }

    public override string ToString()
    {
        return $"OpenIdCredentialsExt({base.ToString()}; access_token='{OpenIdAccessToken}')";
    }
}