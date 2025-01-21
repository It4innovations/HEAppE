using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// Lexis credentials ext
/// </summary>
[DataContract(Name = "LexisCredentialsExt")]
[Description("Lexis credentials ext")]
public class LexisCredentialsExt : AuthenticationCredentialsExt
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
        return $"LexisCredentialsExt({base.ToString()}; access_token='{OpenIdAccessToken}')";
    }
}